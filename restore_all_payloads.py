import os
import subprocess

root_dir = "unity-package/Scripts/"
payload_files = [
    "VibeBridgeKernel.cs",
    "VibeBridge_AuditingPayload.cs",
    "VibeBridge_ExportPayload.cs",
    "VibeBridge_ExtrasPayload.cs",
    "VibeBridge_LifecyclePayload.cs",
    "VibeBridge_MaterialPayload.cs",
    "VibeBridge_RegistryPayload.cs",
    "VibeBridge_StandardPayload.cs",
    "VibeBridge_VisionPayload.cs",
    "VibeBridge_VRChatPayload.cs",
    "VibeBridgeKernel.Utils.cs",
    "VibeBridgeKernel.Telemetry.cs"
]

for filename in payload_files:
    file_path = os.path.join(root_dir, filename)
    print(f"Restoring {file_path} from safety snapshot...")
    try:
        # Get original content from the first commit in .git_safety
        content = subprocess.check_output(["git", "--git-dir=.git_safety", "--work-tree=.", "show", "HEAD:" + file_path]).decode('utf-8')
        with open(file_path, 'w') as f:
            f.write(content)
        print(f"  Successfully restored {filename}")
    except Exception as e:
        print(f"  Failed to restore {filename}: {e}")

