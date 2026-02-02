import os
import datetime
from mcp.server.fastmcp import FastMCP
from ipc.airlock import UnityAirlock
from vibe_logging.logger import VibeLogger
from core.workspace import VibeWorkspaceManager

class VibeEngine:
    def __init__(self, name="UnityVibeBridge"):
        self.mcp = FastMCP(name)
        self.bridge_root = os.getcwd()
        self.workspace = VibeWorkspaceManager(self.bridge_root)
        
        # Current Active Context
        self.project_path = self.workspace.get_active_path()
        self.logger = VibeLogger(self.project_path)
        self.airlock = UnityAirlock(self.project_path, self.logger, self.workspace)
        
        self.activity_file = os.path.join(self.bridge_root, "metadata", "bridge_activity.txt")
        self._ensure_infrastructure()

    def _ensure_infrastructure(self):
        # Ensure infrastructure in BOTH the bridge and the active project
        DIRS = ["metadata", "logs", "captures"]
        for d in DIRS:
            os.makedirs(os.path.join(self.bridge_root, d), exist_ok=True)
            
        # Project-specific DIRS
        PROJECT_DIRS = ["vibe_queue/inbox", "vibe_queue/outbox", "metadata", "logs", "optimizations"]
        for d in PROJECT_DIRS:
            os.makedirs(os.path.join(self.project_path, d), exist_ok=True)

    def switch_project(self, name):
        """Switches the engine's active focus to a different registered project."""
        if self.workspace.set_active_project(name):
            self.project_path = self.workspace.get_active_path()
            # Re-initialize context for the new project
            self.logger = VibeLogger(self.project_path)
            self.airlock = UnityAirlock(self.project_path, self.logger)
            self._ensure_infrastructure()
            return True
        return False

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
