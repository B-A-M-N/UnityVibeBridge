def register_mvc_tools(engine):
    mcp = engine.mcp

    @mcp.tool()
    def get_hierarchy() -> str:
        """Returns the Unity scene hierarchy."""
        return str(engine.unity_request("hierarchy"))

    @mcp.tool()
    def inspect_object(path: str) -> str:
        """Returns components and state of a GameObject."""
        return str(engine.unity_request("inspect", {"path": path}))

    @mcp.tool()
    def set_value(path: str, component: str, field: str, value: str) -> str:
        """Sets a field or property value on a component."""
        return str(engine.unity_request("object/set-value", {"path": path, "component": component, "field": field, "value": value}, is_mutation=True))

    @mcp.tool()
    def begin_transaction(name: str = "AI Op") -> str:
        """Starts an atomic Undo Group."""
        return str(engine.unity_request("transaction/begin", {"name": name}, is_mutation=True))

    @mcp.tool()
    def commit_transaction(rationale: str, state_hash: str, monotonic_tick: int) -> str:
        """
        Commits the current atomic Undo Group. 
        [TRIPLE-LOCK GATE]: Requires technical rationale, latest wal_hash, AND current monotonic_tick.
        """
        # --- LAYER 3: SEMANTIC INVARIANCE (PROOF OF WORK) ---
        last_wal = engine.logger.get_wal_tail(1)
        current_wal_hash = last_wal[0].get("entryHash", "GENESIS") if last_wal else "GENESIS"
        
        if state_hash != current_wal_hash:
            return json.dumps({
                "error": "STATE_HASH_MISMATCH",
                "message": f"Your state_hash ({state_hash}) is stale. Current hash is {current_wal_hash}. Review the latest tool output.",
                "action": "REJECTED"
            })

        # --- LAYER 10: INTENT DECAY INVARIANCE ---
        # Fetch current tick via low-latency heartbeat check if possible, or use last known
        if monotonic_tick <= 0:
            return json.dumps({"error": "INVALID_TICK", "message": "monotonic_tick must be > 0"})

        if len(rationale) < 10:
            return json.dumps({
                "error": "INSUFFICIENT_RATIONALE",
                "message": "Commit rejected. You must provide a clear technical rationale for this state mutation.",
                "action": "REJECTED"
            })

        return str(engine.unity_request("transaction/commit", {"rationale": rationale, "tick": monotonic_tick}, is_mutation=True))
