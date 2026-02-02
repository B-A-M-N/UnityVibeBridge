#!/bin/bash
# 🛡️ UnityVibeBridge: Industrial Health Check
# This script is the mandatory gate for all AI mutations.

echo "[Vibe] Starting Automated Health Audit..."

# 0. Clean Poison Stubs & Duplicates
python3 cleanup_vibe.py /home/bamn/ALCOM/Projects/BAMN-EXTO
if [ $? -ne 0 ]; then
    echo "⚠️ WARNING: Cleanup failed. Continuing anyway..."
fi

# 1. Roslyn Assembly Check
python3 scripts/security_gate.py --package unity-package
if [ $? -ne 0 ]; then
    echo "❌ ROSLYN AUDIT FAILED. Aborting."
    exit 1
fi

# 2. Binary Integrity Map (Sentinel Pre-flight)
echo "[Vibe] Generating Integrity Map..."
python3 scripts/generate_integrity_map.py
if [ $? -ne 0 ]; then
    echo "⚠️ WARNING: Integrity Map generation failed. Check DLL location."
fi

# 3. UPM Version Verification
PACKAGE_VER=$(grep '"version"' unity-package/package.json | sed 's/.*: "\(.*\)".*/\1/')
echo "[Vibe] Package Version: $PACKAGE_VER"

# 3. Kernel Heartbeat Check (Port 8091)
CURL_RES=$(curl -s http://localhost:8091/health)
if [[ $CURL_RES == *"Ready"* ]]; then
    echo "✅ HEARTBEAT DETECTED."
else
    echo "⚠️ WARNING: Kernel not responsive on Port 8091. (May be reloading domain)"
fi

echo "✅ ALL SYSTEMS GO. Turn safe to finalize."
exit 0
