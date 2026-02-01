#!/usr/bin/env python3
import subprocess
import sys
import os
import datetime

def safety_snapshot(message):
    """
    Automated 'Iron Box' snapshot to the .git_safety repository.
    Ensures that every critical AI mutation is recorded in a separate git tree.
    """
    project_root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    git_dir = os.path.join(project_root, ".git_safety")
    
    if not os.path.exists(git_dir):
        print("ERROR: .git_safety directory missing. Snapshot failed.")
        return False

    try:
        # 1. Add changes to the safety tree
        subprocess.run(
            ["git", f"--git-dir={git_dir}", f"--work-tree={project_root}", "add", "."],
            check=True, capture_output=True
        )
        
        # 2. Commit with a timestamped message
        timestamp = datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        full_message = f"[AUTO-SNAPSHOT] {timestamp}: {message}"
        
        subprocess.run(
            ["git", f"--git-dir={git_dir}", f"--work-tree={project_root}", "commit", "-m", full_message],
            check=True, capture_output=True
        )
        
        print(f"SUCCESS: Safety snapshot created: {full_message}")
        return True
    except subprocess.CalledProcessError as e:
        # If there are no changes to commit, git returns exit code 1
        if b"nothing to commit" in e.stdout or b"nothing to commit" in e.stderr:
            return True
        print(f"ERROR: Snapshot failed: {e.stderr.decode()}")
        return False

if __name__ == "__main__":
    msg = sys.argv[1] if len(sys.argv) > 1 else "Generic Hardening Snapshot"
    safety_snapshot(msg)
