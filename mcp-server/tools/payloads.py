def register_payload_tools(engine):
    mcp = engine.mcp

    @mcp.tool()
    def audit_avatar(path: str) -> str:
        """[Payload] Returns a report on meshes and materials."""
        return str(engine.unity_request("audit/avatar", {"path": path}, intent="AUDIT"))

    @mcp.tool()
    def crush_textures(path: str, max_size: int = 512) -> str:
        """[Payload] Downscales textures."""
        return str(engine.unity_request("texture/crush", {"path": path, "maxSize": max_size}, is_mutation=True, intent="OPTIMIZE"))

    @mcp.tool()
    def register_object(path: str, role: str, group: str = "default", slot_index: int = 0) -> str:
        """[Payload] Persists a semantic role for an object (e.g. 'MainBody')."""
        return str(engine.unity_request("registry/add", {"path": path, "role": role, "group": group, "slotIndex": slot_index}, is_mutation=True, intent="REGISTRY"))

    @mcp.tool()
    def get_state_hash() -> str:
        """[Auditing] Returns a snapshot hash of the scene state for multi-agent coordination."""
        return str(engine.unity_request("system/state-hash"))

    @mcp.tool()
    def verify_identity_parity(references: str) -> str:
        """
        Instantly verifies if a list of objects (comma-separated UUIDs or sem:Roles) still exist.
        Returns a simple FOUND/MISSING status for each.
        """
        refs = [r.strip() for h in references.split(",")]
        results = {}
        for r in refs:
            # Quick check via inspect primitive
            res = engine.unity_request("inspect", {"path": r})
            results[r] = "FOUND" if "error" not in res else "MISSING"
        return json.dumps(results)
        
    @mcp.tool()
    def list_available_tools() -> str:
        """Returns a list of all installed VibeTools."""
        return str(engine.unity_request("system/list-tools"))

    @mcp.tool()
    def update_derived_belief(key: str, statement: str, provenance_hashes: str) -> str:
        """
        [EPISTEMIC] Records a derived conclusion in the Belief Ledger.
        Requirement: Must provide comma-separated WAL hashes as provenance.
        """
        hashes = [h.strip() for h in provenance_hashes.split(",")]
        # Fetch current tick from latest unity context if available
        tick = 0
        try:
            # Fallback to local heartbeat if necessary
            with open(os.path.join(engine.project_path, "metadata", "vibe_health.json"), "r") as f:
                tick = json.load(f).get("monotonicTick", 0)
        except: pass
        
        engine.logger.update_belief(key, statement, hashes, tick)
        return json.dumps({"message": f"Belief '{key}' committed to ledger with provenance."})
