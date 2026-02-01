import os

files = [
    "/home/bamn/ALCOM/Projects/BAMN-EXTO/Assets/✮exto/Centiped tail/Centiped tail texture/Centiped tail.mat",
    "/home/bamn/ALCOM/Projects/BAMN-EXTO/Assets/✮exto/mat/harness.mat"
]

for file_path in files:
    if os.path.exists(file_path):
        with open(file_path, 'r') as f:
            content = f.read()
        
        # Set Optimizer back to enabled
        content = content.replace("_ShaderOptimizerEnabled: 0", "_ShaderOptimizerEnabled: 1")
        # Unset ForgotToLock
        content = content.replace("_ForgotToLockMaterial: 1", "_ForgotToLockMaterial: 0")
        
        with open(file_path, 'w') as f:
            f.write(content)
        print(f"Relocked {file_path}")
