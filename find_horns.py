import json

with open('current_hierarchy.json', 'r') as f:
    data = json.load(f)

for node in data.get('nodes', []):
    name = node.get('name', '').lower()
    if 'horn' in name or 'head' in name or 'body' in name:
        print(f"ID: {node.get('instanceID')}, Name: {node.get('name')}, Path: {node.get('path')}")
