namespace GatherAndGrow.Network;

public enum MessageType : byte
{
    PlayerJoined,
    PlayerLeft,
    PlayerMove,
    GatherRequest,
    GatherResult,
    WorldState,
    ToolUpgradeRequest,
    ToolUpgraded,
    GameWon
}
