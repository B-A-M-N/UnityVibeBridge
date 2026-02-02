import os
import sys
import platform

def get_unity_log_path():
    """Locates the Unity Editor.log based on the current OS."""
    if platform.system() == "Windows":
        return os.path.expandvars(r"%LOCALAPPDATA%\Unity\Editor\Editor.log")
    elif platform.system() == "Darwin": # macOS
        return os.path.expanduser("~/Library/Logs/Unity/Editor.log")
    else: # Linux
        return os.path.expanduser("~/.config/unity3d/Editor.log")

def tail_unity_log(lines=1000):
    """Reads the final N lines of the Unity log using efficient seek()."""
    log_path = get_unity_log_path()
    if not os.path.exists(log_path):
        return [f"Error: Log not found at {log_path}"]

    try:
        with open(log_path, "rb") as f:
            f.seek(0, os.SEEK_END)
            end_pos = f.tell()
            buffer = 1024 * 10 # 10KB chunks
            pos = end_pos
            data = b""
            
            while len(data.splitlines()) <= lines and pos > 0:
                pos = max(0, pos - buffer)
                f.seek(pos)
                data = f.read(end_pos - pos)
            
            return [l.decode('utf-8', errors='ignore') for l in data.splitlines()[-lines:]]
    except Exception as e:
        return [f"Error reading log: {str(e)}"]

def find_compiler_errors():
    """Scans the tail for C# compiler errors."""
    log_lines = tail_unity_log(500)
    errors = []
    # Pattern: Assets/Path/To/File.cs(line,col): error CSXXXX: Message
    error_pattern = re.compile(r"(Assets/.*\.cs)\(\d+,\d+\): error CS\d+:")
    
    for line in log_lines:
        if "error CS" in line:
            errors.append(line.strip())
    return errors

if __name__ == "__main__":
    import re
    print(f"--- TAILING UNITY LOG: {get_unity_log_path()} ---")
    errs = find_compiler_errors()
    if errs:
        print(f"❌ Found {len(errs)} Compiler Errors:")
        for e in errs: print(f"  {e}")
    else:
        print("✅ No immediate compiler errors found in log tail.")
