#!/bin/bash

# UnityVibeBridge: The Governed Creation Kernel for Unity
# Copyright (C) 2026 B-A-M-N
#
# This software is dual-licensed under the GNU AGPLv3 and a 
# Commercial "Work-or-Pay" Maintenance Agreement.
#
# You may use this file under the terms of the AGPLv3, provided 
# you meet all requirements (including source disclosure).
#
# For commercial use, or to keep your modifications private, 
# you must satisfy the requirements of the Commercial Path 
# as defined in the LICENSE file at the project root.

# UnityVibeBridge Sandbox Launcher
# This script builds and runs the sandboxed Gemini/Goose agent.

IMAGE_NAME="unity-vibe-sandbox"

echo "🛠 Building the Sandbox (this may take a few minutes on first run)..."
docker build -t $IMAGE_NAME -f Dockerfile.sandbox .

if [ $? -ne 0 ]; then
    echo "❌ Build failed. Ensure Docker is installed and running."
    exit 1
fi

echo "🚀 Launching Sandbox..."
echo "--------------------------------------------------------"
echo "🔒 PROTECTION: Your OS is now hidden from the agent."
echo "📂 SCOPE: The agent can only see this project folder."
echo "🔗 NETWORK: The agent can talk to Unity on localhost:8085."
echo "--------------------------------------------------------"

# Run the container
# --network host: Allows agent to talk to Unity on localhost
# -v $(pwd):/workspace: Maps the project files
docker run -it --rm \
    --network host \
    -v "$(pwd):/workspace" \
    -e GOOGLE_API_KEY=$GOOGLE_API_KEY \
    -e OPENAI_API_KEY=$OPENAI_API_KEY \
    -e ANTHROPIC_API_KEY=$ANTHROPIC_API_KEY \
    $IMAGE_NAME
