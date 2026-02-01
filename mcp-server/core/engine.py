import os
from mcp.server.fastmcp import FastMCP
from ipc.airlock import UnityAirlock
from vibe_logging.logger import VibeLogger

class VibeEngine:
    def __init__(self, name="UnityVibeBridge"):
        self.mcp = FastMCP(name)
        self.project_path = os.getcwd()
        self.logger = VibeLogger(self.project_path)
        self.airlock = UnityAirlock(self.project_path, self.logger)
        self.activity_file = os.path.join(self.project_path, "metadata", "bridge_activity.txt")
        self._ensure_infrastructure()

    def _ensure_infrastructure(self):
        DIRS = ["vibe_queue/inbox", "vibe_queue/outbox", "metadata", "captures", "logs", "optimizations"]
        for d in DIRS:
            path = os.path.join(self.project_path, d)
            if not os.path.exists(path):
                os.makedirs(path)

    def set_activity(self, label):
        """Writes a visible thought marker for the human operator."""
        try:
            with open(self.activity_file, "w") as f:
                f.write(f"[{datetime.datetime.now().strftime('%H:%M:%S')}] {label}")
        except: pass

    def unity_request(self, path, params=None, is_mutation=False, intent=None):
        if intent: self.set_activity(f"AI_{intent}: {path}")
        res = self.airlock.request(path, params, is_mutation, intent)
        self.set_activity("IDLE")
        return res

    def run(self):
        self.mcp.run()
