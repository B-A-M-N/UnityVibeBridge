import os

root_dir = "/home/bamn/ALCOM/Projects/BAMN-EXTO/Assets/✮exto"
for root, dirs, files in os.walk(root_dir):
    for filename in files:
        if filename.endswith(".mat"):
            file_path = os.path.join(root, filename)
            try:
                with open(file_path, 'r') as f:
                    content = f.read()
                
                # Set Optimizer to disabled (0) and Set ForgotToLock (1)
                new_content = content.replace("_ShaderOptimizerEnabled: 1", "_ShaderOptimizerEnabled: 0")
                new_content = new_content.replace("_ForgotToLockMaterial: 0", "_ForgotToLockMaterial: 1")
                
                if new_content != content:
                    with open(file_path, 'w') as f:
                        f.write(new_content)
                    print(f"Unlocked {file_path}")
            except Exception as e:
                print(f"Failed to process {file_path}: {e}")
