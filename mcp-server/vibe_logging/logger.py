import os
import json
import datetime

class VibeLogger:
    def __init__(self, project_path):
        self.log_dir = os.path.join(project_path, "logs")
        self.wal_path = os.path.join(self.log_dir, "vibe_audit.jsonl")
        self.belief_path = os.path.join(self.log_dir, "vibe_beliefs.json")
        self.entropy_budget = 100
        self.entropy_used = 0
        self.drift_budget = 5 # Allowed deviations before human intervention
        self.idempotency_map = {}
        self.belief_ledger = {} # key -> {belief, provenance, confidence, expires_at_tick}
        if not os.path.exists(self.log_dir):
            os.makedirs(self.log_dir)
        self._load_beliefs()

    def update_belief(self, key, statement, provenance, current_tick, ttl=100):
        """Updates the belief ledger with provenance and TTL (Confidence Decay)."""
        self.belief_ledger[key] = {
            "statement": statement,
            "provenance": provenance, # list of hashes
            "confidence": 1.0,
            "updated_at": datetime.datetime.utcnow().isoformat(),
            "expires_at_tick": current_tick + ttl
        }
        self._save_beliefs()

    def get_active_beliefs(self, current_tick):
        """Returns non-expired beliefs."""
        active = {}
        for k, v in self.belief_ledger.items():
            if v["expires_at_tick"] > current_tick:
                # Apply linear confidence decay
                remaining = v["expires_at_tick"] - current_tick
                v["confidence"] = max(0, remaining / 100.0)
                active[k] = v
        return active

    def _load_beliefs(self):
        if os.path.exists(self.belief_path):
            try:
                with open(self.belief_path, "r") as f: self.belief_ledger = json.load(f)
            except: self.belief_ledger = {}

    def _save_beliefs(self):
        with open(self.belief_path, "w") as f: json.dump(self.belief_ledger, f, indent=2)

    def log_intent(self, intent, details):
        """Logs a high-level intent for Agent Alpha/Beta coordination."""
        self.entropy_used += 1 # Every intent consumes entropy
        entry = {
            "timestamp": datetime.datetime.utcnow().isoformat(),
            "type": "INTENT",
            "intent": intent,
            "details": details,
            "entropy_remaining": self.entropy_budget - self.entropy_used
        }
        self._write(entry)

    def is_idempotent(self, key, current_payload_hash):
        if key in self.idempotency_map:
            return self.idempotency_map[key] == current_payload_hash
        return False

    def record_idempotency(self, key, response_hash):
        self.idempotency_map[key] = response_hash

    def log_mutation(self, action, payload, response):
        """Logs a concrete mutation to the WAL."""
        entry = {
            "timestamp": datetime.datetime.utcnow().isoformat(),
            "type": "MUTATION",
            "action": action,
            "payload": payload,
            "response": response
        }
        self._write(entry)

    def get_wal_tail(self, count=10):
        if not os.path.exists(self.wal_path): return []
        try:
            with open(self.wal_path, "r") as f:
                lines = f.readlines()
                return [json.loads(l) for l in lines[-count:]]
        except: return []

    def _write(self, entry):
        with open(self.wal_path, "a") as f:
            f.write(json.dumps(entry) + "\n")

