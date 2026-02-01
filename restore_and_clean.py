import os
import subprocess

target_dir = "/home/bamn/ALCOM/Projects/BAMN-EXTO/Assets/VibeBridge/"
safety_dir = "unity-package/Scripts/"

# Get the list of files that WERE there in the initial safety commit
files_to_restore = subprocess.check_output(["git", "--git-dir=.git_safety", "--work-tree=.", "ls-tree", "-r", "--name-only", "HEAD", safety_dir]).decode().splitlines()

for f in files_to_restore:
    # f looks like "unity-package/Scripts/VibeBridgeKernel.cs"
    filename = os.path.basename(f)
    print(f"Restoring {filename}...")
    content = subprocess.check_output(["git", "--git-dir=.git_safety", "--work-tree=.", "show", "HEAD:" + f])
    with open(os.path.join(target_dir, filename), 'wb') as out:
        out.write(content)

