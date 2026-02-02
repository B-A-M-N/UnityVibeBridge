import os, json, uuid
cmd_id = str(uuid.uuid4())
payload = {'action': 'system/api_dump', 'id': cmd_id, 'capability': 'read'}
os.makedirs('vibe_queue/inbox', exist_ok=True)
with open(f'vibe_queue/inbox/{cmd_id}.json', 'w') as f:
    json.dump(payload, f)
print(f"Triggered: {cmd_id}")
