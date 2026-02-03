import os
import json
import uuid
import time
import requests
import datetime
import sys

# Ensure scripts directory is accessible for the gate
sys.path.append(os.path.join(os.path.dirname(__file__), "..", ".."))
try:
    from scripts.security_gate import SecurityGate
except ImportError:
    # Fallback for different execution contexts
    sys.path.append(os.getcwd())
    from scripts.security_gate import SecurityGate

from .sentinel import BinarySentinel

class UnityAirlock:
    def __init__(self, project_path, logger, workspace=None):
        self.project_path = project_path
        self.logger = logger
        self.workspace = workspace
        # Paths are now resolved relative to the active project path provided by the engine
        self.inbox = os.path.join(project_path, "vibe_queue", "inbox")
        self.outbox = os.path.join(project_path, "vibe_queue", "outbox")
        self.status_file = os.path.join(project_path, "metadata", "vibe_status.json")
        self.health_file = os.path.join(project_path, "metadata", "vibe_health.json")
        
        self.sentinel = BinarySentinel(project_path, logger)
        self.sentinel.verify()

    def _audit_payload(self, path, params):
        """Performs recursive in-memory AST and keyword analysis on tool parameters."""
        if not params: return []
        issues = []
        
        # Get all allowed roots from the workspace
        safe_zones = self.workspace.get_safe_zones() if self.workspace else [os.getcwd()]
        
        # 1. Scan all string parameters for C# and general security violations
        for key, value in params.items():
            if isinstance(value, str):
                # Only check for C# if it looks like code (contains keywords or common patterns)
                code_patterns = ["using ", "namespace ", "class ", "void ", "{", "public ", "static "]
                if any(p in value for p in code_patterns):
                    issues.extend(SecurityGate.check_csharp(value))
                
                # Check for path safety across all zones
                if not SecurityGate.is_path_safe(value, safe_zones):
                     issues.append(f"Security Violation: Access to forbidden path '{value}' blocked.")
                # Check for sensitive secret patterns
                issues.extend(SecurityGate._check_secrets(value))
                
        # 2. Path-specific deep auditing
        if path == "system/execute-recipe":
            try:
                recipe = json.loads(params.get("data", "{}"))
                for tool in recipe.get("tools", []):
                    # Recursive audit of tools within recipes
                    issues.extend(self._audit_payload(tool.get("action"), 
                                 dict(zip(tool.get("keys", []), tool.get("values", [])))))
            except:
                issues.append("Security Violation: Malformed JSON in recipe data.")

        return [i for i in issues if i]

    def request(self, path, params=None, is_mutation=False, intent=None):
        """Secure AIRLOCK IPC with Triple-Lock, In-Process Auditing, and Smart-Wait."""
        
        # --- LAYER -1: BINARY SENTINEL (Outside-In Integrity) ---
        if is_mutation and not self.sentinel.is_verified:
            report = self.sentinel.get_status_report()
            return json.dumps({
                "error": "INTEGRITY_FAILURE",
                "mode": report["mode"],
                "details": report["error"],
                "message": "Mutation blocked: Binary Integrity check failed (Possible tampering)."
            })

        # --- LAYER 0: PRE-FLIGHT SECURITY GATE ---
        if is_mutation:
            audit_errors = self._audit_payload(path, params)
            if audit_errors:
                self.logger.log_intent("SECURITY_BLOCK", {"path": path, "errors": audit_errors})
                return json.dumps({
                    "error": "SECURITY_VIOLATION",
                    "details": audit_errors,
                    "message": "Mutation blocked by In-Process Security Gate."
                })

        capability = "Admin" if is_mutation else "Read"
        token = self._get_token()
        
        # --- REALITY FIX: SMART-WAIT FOR COMPILATION ---
        # If we are mutating, poll for 'Ready' state for up to 10 seconds before failing.
        wait_start = time.time()
        while is_mutation and time.time() - wait_start < 10:
            status = self._get_vibe_status()
            if status == "Ready": break
            if status == "VETOED": 
                return json.dumps({"error": "VETOED", "message": "Kernel locked by human veto."})
            time.sleep(1.0) # Wait for compiler/importer

        data = None
        meta_wrapper = {}
        # Try HTTP first
        try:
            headers = {
                "X-Vibe-Token": token or "FORCE_WAKE", # Resilience Fix
                "X-Vibe-Capability": capability,
                "Content-Type": "application/json"
            }
            # The kernel expects requests on /vibe via POST
            cmd_payload = {
                "action": path,
                "capability": capability,
                "keys": list(params.keys()) if params else [],
                "values": [str(v) for v in params.values()] if params else []
            }
            resp = requests.post(f"http://127.0.0.1:8091/vibe", data=json.dumps(cmd_payload), headers=headers, timeout=5)
            if resp.status_code == 200:
                raw_data = resp.json()
                if isinstance(raw_data, list):
                    data = {"results": raw_data}
                elif isinstance(raw_data, dict) and "payload" in raw_data:
                    meta_wrapper = raw_data
                    data = json.loads(meta_wrapper.get("payload", "{}"))
                else:
                    data = raw_data
                    if isinstance(raw_data, dict): meta_wrapper = raw_data
        except Exception as e:
            # print(f"HTTP Request failed: {e}")
            pass

        if data is None:
            raw_wrapper = self._filesystem_request(path, params, capability, is_mutation)
            if isinstance(raw_wrapper, dict):
                meta_wrapper = raw_wrapper
                data = json.loads(meta_wrapper.get("payload", "{}"))
            elif isinstance(raw_wrapper, list):
                data = {"results": raw_wrapper}
            else:
                data = {"raw": raw_wrapper}
            
        # --- LAYER 1: TYPE INVARIANT ENFORCEMENT ---
        if not isinstance(data, dict):
            data = {"results": data}

        data["_monotonicTick"] = meta_wrapper.get("monotonicTick", 0)
        data["_engineState"] = meta_wrapper.get("state", "UNKNOWN")

        # --- LAYER 2, 8 & 9: CONTEXTUAL & COGNITIVE INVARIANCE ---
        wal = self.logger.get_wal_tail(1)
        current_tick = data.get("_monotonicTick", 0)
        
        # REALITY FIX: Use a stable hash that ignores volatile timestamps
        stable_wal_hash = wal[0].get("entryHash", "GENESIS") if wal else "GENESIS"
        
        # Source health metrics
        health = {}
        if os.path.exists(self.health_file):
            try:
                with open(self.health_file, "r") as f: health = json.load(f)
            except: pass

        data["_vibe_invariance"] = {
            "wal_hash": stable_wal_hash,
            "unity_status": self._get_vibe_status(),
            "script_error_count": health.get("script_error_count", 0),
            "engine_state": data.get("_engineState", "UNKNOWN"),
            "entropy_remaining": self.logger.entropy_budget - self.logger.entropy_used,
            "drift_budget": self.logger.drift_budget,
            "active_beliefs": self.logger.get_active_beliefs(current_tick),
            "monotonic_tick": current_tick,
            "schema_version": "v1.5.0",
            "timestamp": datetime.datetime.utcnow().isoformat()
        }

        if is_mutation: 
            self.logger.log_mutation(path, params, data)
        
        return data

    def _get_vibe_status(self):
        """Reads the mechanical status of Unity."""
        if os.path.exists(self.status_file):
            try:
                with open(self.status_file, "r") as f:
                    return json.load(f).get("state", "Unknown")
            except: pass
        return "Offline"

    def _get_token(self):
        if os.path.exists(self.status_file):
            try:
                with open(self.status_file, "r") as f:
                    return json.load(f).get("nonce")
            except: pass
        return None

    def _filesystem_request(self, path, params, capability, is_mutation):
        cmd_id = str(uuid.uuid4())
        payload = {
            "action": path,
            "id": cmd_id,
            "capability": capability,
            "keys": list(params.keys()) if params else [],
            "values": [str(v) for v in params.values()] if params else []
        }

        with open(os.path.join(self.inbox, f"{cmd_id}.json"), "w") as f:
            json.dump(payload, f)

        outbox_file = os.path.join(self.outbox, f"res_{cmd_id}.json")
        start = time.time()
        while time.time() - start < 15:
            if os.path.exists(outbox_file):
                with open(outbox_file, "r") as f: res_content = f.read()
                os.remove(outbox_file)
                try:
                    data = json.loads(res_content)
                except:
                    data = {"raw_response": res_content}
                
                if is_mutation: self.logger.log_mutation(path, params, data)
                return data
            time.sleep(0.1)
        return {"error": "Timeout"}
