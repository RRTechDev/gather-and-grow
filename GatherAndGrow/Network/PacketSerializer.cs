using System.Numerics;
using GatherAndGrow.Game;

namespace GatherAndGrow.Network;

public static class PacketSerializer
{
    // --- Write helpers ---

    public static byte[] WritePlayerJoined(ulong steamId, string name, int colorIndex)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write((byte)MessageType.PlayerJoined);
        w.Write(steamId);
        w.Write(name);
        w.Write(colorIndex);
        return ms.ToArray();
    }

    public static byte[] WritePlayerLeft(ulong steamId)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write((byte)MessageType.PlayerLeft);
        w.Write(steamId);
        return ms.ToArray();
    }

    public static byte[] WritePlayerMove(ulong steamId, Vector2 position)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write((byte)MessageType.PlayerMove);
        w.Write(steamId);
        w.Write(position.X);
        w.Write(position.Y);
        return ms.ToArray();
    }

    public static byte[] WriteGatherRequest(ulong steamId, int nodeId)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write((byte)MessageType.GatherRequest);
        w.Write(steamId);
        w.Write(nodeId);
        return ms.ToArray();
    }

    public static byte[] WriteGatherResult(ulong steamId, int nodeId, ResourceType resourceType, int amount)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write((byte)MessageType.GatherResult);
        w.Write(steamId);
        w.Write(nodeId);
        w.Write((byte)resourceType);
        w.Write(amount);
        return ms.ToArray();
    }

    public static byte[] WriteWorldState(GameState state)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write((byte)MessageType.WorldState);

        // Nodes
        w.Write(state.ResourceNodes.Count);
        foreach (var node in state.ResourceNodes)
        {
            w.Write(node.Id);
            w.Write((byte)node.Type);
            w.Write(node.Position.X);
            w.Write(node.Position.Y);
            w.Write(node.RemainingAmount);
            w.Write(node.MaxAmount);
            w.Write(node.RespawnTimer);
        }

        // Players
        w.Write(state.Players.Count);
        foreach (var kvp in state.Players)
        {
            var p = kvp.Value;
            w.Write(p.SteamId);
            w.Write(p.Name);
            w.Write(p.Position.X);
            w.Write(p.Position.Y);
            w.Write(p.Color.R);
            w.Write(p.Color.G);
            w.Write(p.Color.B);
            w.Write(p.Color.A);

            // Inventory
            w.Write(p.Inventory[ResourceType.Wood]);
            w.Write(p.Inventory[ResourceType.Iron]);
            w.Write(p.Inventory[ResourceType.Gold]);

            // Tool levels
            w.Write(p.ToolLevels[ToolType.Axe]);
            w.Write(p.ToolLevels[ToolType.Pickaxe]);
            w.Write(p.ToolLevels[ToolType.GoldPick]);

            // Gathering state
            w.Write(p.GatheringNodeId.HasValue);
            w.Write(p.GatheringNodeId ?? -1);
            w.Write(p.GatherProgress);
        }

        return ms.ToArray();
    }

    public static byte[] WriteToolUpgradeRequest(ulong steamId, ToolType tool)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write((byte)MessageType.ToolUpgradeRequest);
        w.Write(steamId);
        w.Write((byte)tool);
        return ms.ToArray();
    }

    public static byte[] WriteToolUpgraded(ulong steamId, ToolType tool, int newLevel)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write((byte)MessageType.ToolUpgraded);
        w.Write(steamId);
        w.Write((byte)tool);
        w.Write(newLevel);
        return ms.ToArray();
    }

    public static byte[] WriteGameWon(ulong winnerId)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write((byte)MessageType.GameWon);
        w.Write(winnerId);
        return ms.ToArray();
    }

    // --- Read helpers ---

    public static (ulong SteamId, string Name, int ColorIndex) ReadPlayerJoined(BinaryReader r)
    {
        return (r.ReadUInt64(), r.ReadString(), r.ReadInt32());
    }

    public static ulong ReadPlayerLeft(BinaryReader r)
    {
        return r.ReadUInt64();
    }

    public static (ulong SteamId, Vector2 Position) ReadPlayerMove(BinaryReader r)
    {
        ulong id = r.ReadUInt64();
        float x = r.ReadSingle();
        float y = r.ReadSingle();
        return (id, new Vector2(x, y));
    }

    public static (ulong SteamId, int NodeId) ReadGatherRequest(BinaryReader r)
    {
        return (r.ReadUInt64(), r.ReadInt32());
    }

    public static (ulong SteamId, int NodeId, ResourceType Type, int Amount) ReadGatherResult(BinaryReader r)
    {
        ulong id = r.ReadUInt64();
        int nodeId = r.ReadInt32();
        var type = (ResourceType)r.ReadByte();
        int amount = r.ReadInt32();
        return (id, nodeId, type, amount);
    }

    public static void ReadWorldState(BinaryReader r, GameState state)
    {
        // Nodes
        int nodeCount = r.ReadInt32();
        // Resize if needed
        while (state.ResourceNodes.Count < nodeCount)
            state.ResourceNodes.Add(new ResourceNode());

        for (int i = 0; i < nodeCount; i++)
        {
            var node = state.ResourceNodes[i];
            node.Id = r.ReadInt32();
            node.Type = (ResourceType)r.ReadByte();
            node.Position = new Vector2(r.ReadSingle(), r.ReadSingle());
            node.RemainingAmount = r.ReadInt32();
            node.MaxAmount = r.ReadInt32();
            node.RespawnTimer = r.ReadSingle();
        }

        // Trim excess nodes if any
        while (state.ResourceNodes.Count > nodeCount)
            state.ResourceNodes.RemoveAt(state.ResourceNodes.Count - 1);

        // Players
        int playerCount = r.ReadInt32();
        var seen = new HashSet<ulong>();

        for (int i = 0; i < playerCount; i++)
        {
            ulong steamId = r.ReadUInt64();
            string name = r.ReadString();
            float px = r.ReadSingle();
            float py = r.ReadSingle();
            byte cr = r.ReadByte();
            byte cg = r.ReadByte();
            byte cb = r.ReadByte();
            byte ca = r.ReadByte();

            int wood = r.ReadInt32();
            int iron = r.ReadInt32();
            int gold = r.ReadInt32();

            int axeLevel = r.ReadInt32();
            int pickLevel = r.ReadInt32();
            int goldPickLevel = r.ReadInt32();

            bool hasGathering = r.ReadBoolean();
            int gatherNodeId = r.ReadInt32();
            float gatherProgress = r.ReadSingle();

            seen.Add(steamId);

            if (!state.Players.TryGetValue(steamId, out var player))
            {
                player = new Player { SteamId = steamId };
                state.Players[steamId] = player;
            }

            player.Name = name;
            // Only overwrite position for remote players (local player uses prediction)
            if (steamId != state.LocalSteamId)
            {
                player.Position = new Vector2(px, py);
            }
            player.Color = new Raylib_cs.Color(cr, cg, cb, ca);
            player.Inventory[ResourceType.Wood] = wood;
            player.Inventory[ResourceType.Iron] = iron;
            player.Inventory[ResourceType.Gold] = gold;
            player.ToolLevels[ToolType.Axe] = axeLevel;
            player.ToolLevels[ToolType.Pickaxe] = pickLevel;
            player.ToolLevels[ToolType.GoldPick] = goldPickLevel;
            player.GatheringNodeId = hasGathering ? gatherNodeId : null;
            player.GatherProgress = gatherProgress;
        }

        // Remove players that left
        var toRemove = state.Players.Keys.Where(k => !seen.Contains(k)).ToList();
        foreach (var id in toRemove)
            state.Players.Remove(id);
    }

    public static (ulong SteamId, ToolType Tool) ReadToolUpgradeRequest(BinaryReader r)
    {
        return (r.ReadUInt64(), (ToolType)r.ReadByte());
    }

    public static (ulong SteamId, ToolType Tool, int NewLevel) ReadToolUpgraded(BinaryReader r)
    {
        return (r.ReadUInt64(), (ToolType)r.ReadByte(), r.ReadInt32());
    }

    public static ulong ReadGameWon(BinaryReader r)
    {
        return r.ReadUInt64();
    }
}
