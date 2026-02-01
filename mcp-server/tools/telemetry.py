def register_telemetry_tools(engine):
    mcp = engine.mcp

    @mcp.tool()
    def get_errors() -> str:
        """
        Retrieves the current error state and hash from the Unity Kernel.
        Use this for Pre-Flight checks to acquire 'state_hash'.
        """
        return str(engine.unity_request("engine/error/state"))
