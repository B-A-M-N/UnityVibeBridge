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

class UnityAirlock:
    def __init__(self, project_path, logger):
        self.project_path = project_path
        self.logger = logger
        self.inbox = os.path.join(project_path, "vibe_queue", "inbox")
        self.outbox = os.path.join(project_path, "vibe_queue", "outbox")
        self.status_file = os.path.join(project_path, "metadata", "vibe_status.json")
        self.health_file = os.path.join(project_path, "metadata", "vibe_health.json")

    def _audit_payload(self, path, params):
        """Performs recursive in-memory AST and keyword analysis on tool parameters."""
        if not params: return []
        issues = []
        
        # 1. Scan all string parameters for C# and general security violations
        for key, value in params.items():
            if isinstance(value, str):
                # Check for C# forbidden patterns (Reflection, etc.)
                issues.extend(SecurityGate.check_csharp(value))
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
        # Try HTTP first
        try:
            headers = {
                "X-Vibe-Token": token or "FORCE_WAKE", # Resilience Fix
                "X-Vibe-Capability": capability
            }
            resp = requests.get(f"http://127.0.0.1:8085/{path}", params=params, headers=headers, timeout=5)
            if resp.status_code == 200:
                wrapper = resp.json()
                data = json.loads(wrapper.get("payload", "{}"))
                data["_monotonicTick"] = wrapper.get("monotonicTick")
                data["_engineState"] = wrapper.get("state")
        except: pass 

        if data is None:
            wrapper = self._filesystem_request(path, params, capability, is_mutation)
            data = json.loads(wrapper.get("payload", "{}"))
            data["_monotonicTick"] = wrapper.get("monotonicTick")
            data["_engineState"] = wrapper.get("state")

        # --- LAYER 2, 8 & 9: CONTEXTUAL & COGNITIVE INVARIANCE ---
        wal = self.logger.get_wal_tail(1)
        current_tick = data.get("_monotonicTick", 0)
        
        # REALITY FIX: Use a stable hash that ignores volatile timestamps
        stable_wal_hash = wal[0].get("entryHash", "GENESIS") if wal else "GENESIS"
        
        data["_vibe_invariance"] = {
            "wal_hash": stable_wal_hash,
            "unity_status": self._get_vibe_status(),
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
            if id_key: 
                payload_hash = str(hash(json.dumps(params, sort_keys=True)))
                self.logger.record_idempotency(id_key, payload_hash)
        
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
