#!/usr/bin/env python3
import os
import glob

def rotate_logs(max_lines=1000):
    """
    Keeps log files from bloating the AI context window.
    Truncates files to the last N lines.
    """
    project_root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    log_dir = os.path.join(project_root, "logs")
    
    if not os.path.exists(log_dir):
        return

    # Skip the audit log (that should be preserved)
    log_files = glob.glob(os.path.join(log_dir, "*.log"))
    
    for log_file in log_files:
        try:
            with open(log_file, 'r') as f:
                lines = f.readlines()
            
            if len(lines) > max_lines:
                with open(log_file, 'w') as f:
                    f.writelines(lines[-max_lines:])
                print(f"Rotated {os.path.basename(log_file)}: Kept last {max_lines} lines.")
        except Exception as e:
            print(f"Failed to rotate {log_file}: {e}")

if __name__ == "__main__":
    rotate_logs()
