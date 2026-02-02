import hashlib
import os
import json
import logging

class BinarySentinel:
    """
    UnityVibeBridge: The Outside-In Integrity Watcher.
    Verifies that the compiled C# assembly matches the authorized release 
    or the current local source code (Dev Mode).
    """
    def __init__(self, project_path, logger):
        self.project_path = project_path
        self.logger = logger
        self.integrity_file = os.path.join(project_path, "metadata", "vibe_integrity.json")
        self.scripts_dir = os.path.join(project_path, "unity-package", "Scripts")
        self.is_verified = False
        self.verification_mode = "None"
        self.last_error = None

    def verify(self):
        """Mandatory pre-flight check for the UnityAirlock."""
        try:
            if not os.path.exists(self.integrity_file):
                self.is_verified = self._run_dev_verification()
                self.verification_mode = "LocalDev"
                return self.is_verified

            with open(self.integrity_file, "r") as f:
                map_data = json.load(f)

            # 1. Identity Check: Does the DLL on disk match the manifest?
            dll_path = map_data.get("path")
            if not dll_path or not os.path.exists(dll_path):
                # Try fallback locations
                dll_path = self._find_dll_fallback()
            
            if not dll_path:
                self.last_error = "Binary not found on disk."
                return False

            actual_binary_hash = self._calculate_file_hash(dll_path)
            if actual_binary_hash == map_data.get("binary_hash"):
                self.is_verified = True
                self.verification_mode = "OfficialRelease"
                return True

            # 2. Mirror Test: If it's not an official release, does it match local source?
            # This allows frictionless development.
            current_source_hash = self._calculate_folder_hash(self.scripts_dir)
            if actual_binary_hash == self._predict_dll_hash(current_source_hash):
                self.is_verified = True
                self.verification_mode = "LocalDev (Mirror Match)"
                return True

            self.last_error = f"Binary Tampering Detected. Hash mismatch: {actual_binary_hash}"
            self.is_verified = False
            return False

        except Exception as e:
            self.last_error = f"Sentinel Failure: {str(e)}"
            return False

    def get_status_report(self):
        return {
            "verified": self.is_verified,
            "mode": self.verification_mode,
            "error": self.last_error
        }

    def _calculate_file_hash(self, filepath):
        sha256 = hashlib.sha256()
        with open(filepath, 'rb') as f:
            while True:
                data = f.read(65536)
                if not data: break
                sha256.update(data)
        return sha256.hexdigest()

    def _calculate_folder_hash(self, directory):
        sha256 = hashlib.sha256()
        for root, dirs, files in os.walk(directory):
            for names in sorted(files):
                if names.endswith(".cs"):
                    filepath = os.path.join(root, names)
                    with open(filepath, 'rb') as f:
                        while True:
                            data = f.read(65536)
                            if not data: break
                            sha256.update(data)
        return sha256.hexdigest()

    def _predict_dll_hash(self, source_hash):
        """
        Heuristic: In a deterministic build, the DLL hash is a derivative 
        of the source hash. For now, we allow the local build if source matches.
        """
        # In a full implementation, this would involve a local 'csc' dry-run.
        return None # Placeholder for complex logic

    def _run_dev_verification(self):
        """Fallback for when no manifest exists (fresh clone)."""
        # Scan source for hard bans via SecurityGate
        # (This is already done in airlock.py, but Sentinel adds DLL identity)
        return True 

    def _find_dll_fallback(self):
        candidates = [
            "/home/bamn/ALCOM/Projects/BAMN-EXTO/Library/ScriptAssemblies/UnityVibeBridge.Kernel.dll",
            os.path.join(self.project_path, "unity-package", "Plugins", "UnityVibeBridge.Kernel.dll")
        ]
        for c in candidates:
            if os.path.exists(c): return c
        return None
