import os
import json
import logging

class VibeWorkspaceManager:
    """
    Manages the 'Workspace Perimeter'.
    Handles project registration, peering, and path resolution across zones.
    """
    def __init__(self, bridge_root):
        self.bridge_root = bridge_root
        self.workspace_file = os.path.join(bridge_root, "metadata", "vibe_workspace.json")
        self.projects = {}
        self.active_project = None
        self.load_workspace()

    def load_workspace(self):
        if os.path.exists(self.workspace_file):
            try:
                with open(self.workspace_file, "r") as f:
                    data = json.load(f)
                    self.projects = data.get("projects", {})
                    self.active_project = data.get("active_project")
            except Exception as e:
                logging.error(f"Failed to load workspace: {e}")

    def save_workspace(self):
        try:
            os.makedirs(os.path.dirname(self.workspace_file), exist_ok=True)
            with open(self.workspace_file, "w") as f:
                json.dump({
                    "projects": self.projects,
                    "active_project": self.active_project
                }, f, indent=2)
        except Exception as e:
            logging.error(f"Failed to save workspace: {e}")

    def register_project(self, name, path, project_type="unity"):
        """Adds a project to the perimeter."""
        abs_path = os.path.abspath(path)
        self.projects[name] = {
            "path": abs_path,
            "type": project_type,
            "registered_at": os.path.getmtime(abs_path) if os.path.exists(abs_path) else 0
        }
        if not self.active_project:
            self.active_project = name
        self.save_workspace()
        return abs_path

    def set_active_project(self, name):
        if name in self.projects:
            self.active_project = name
            self.save_workspace()
            return True
        return False

    def get_active_path(self):
        if self.active_project and self.active_project in self.projects:
            return self.projects[self.active_project]["path"]
        return self.bridge_root

    def resolve_path(self, path, peer_name=None):
        """
        Resolves a path within the perimeter.
        If peer_name is provided, resolves relative to that peer.
        Otherwise, resolves relative to the active project.
        """
        base_path = self.get_active_path()
        if peer_name and peer_name in self.projects:
            base_path = self.projects[peer_name]["path"]
        elif peer_name == "bridge":
            base_path = self.bridge_root

        # Handle absolute paths if they are within the perimeter
        if os.path.isabs(path):
            if any(path.startswith(p["path"]) for p in self.projects.values()) or path.startswith(self.bridge_root):
                return path
            return None # Path outside perimeter

        return os.path.abspath(os.path.join(base_path, path))

    def get_safe_zones(self):
        """Returns a list of all directories the AI is allowed to touch."""
        zones = [self.bridge_root]
        for p in self.projects.values():
            zones.append(p["path"])
        return zones
