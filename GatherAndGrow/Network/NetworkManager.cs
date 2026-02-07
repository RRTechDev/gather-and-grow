using System.Numerics;
using Steamworks;
using Steamworks.Data;
using GatherAndGrow.Game;
using GatherAndGrow.UI;

namespace GatherAndGrow.Network;

public class NetworkManager
{
    private GameState _state;
    public bool SteamInitialized { get; private set; }
    public bool SteamWasDisconnected { get; private set; }
    public string SteamError { get; private set; } = "";
    public string LocalPlayerName { get; private set; } = "";

    // Direct P2P: track connected peers by SteamId
    private HashSet<ulong> _connectedPeers = new();
    private ulong _hostSteamId;

    private float _networkTickTimer;
    private int _nextColorIndex;

    public NetworkManager(GameState state)
    {
        _state = state;
    }

    public bool Init()
    {
        try
        {
            // Shut down any previous failed/partial init before retrying
            if (SteamInitialized)
            {
                SteamClient.Shutdown();
                SteamInitialized = false;
            }

            Console.WriteLine("Attempting SteamClient.Init(480)...");
            SteamClient.Init(480, asyncCallbacks: true);
            SteamInitialized = true;
            _state.LocalSteamId = SteamClient.SteamId.Value;
            LocalPlayerName = SteamClient.Name;
            Console.WriteLine($"Steam initialized! SteamId={_state.LocalSteamId}, Name={LocalPlayerName}");

            // Register P2P session request callback
            SteamNetworking.OnP2PSessionRequest = OnP2PSessionRequest;

            return true;
        }
        catch (Exception ex)
        {
            SteamError = ex.Message;
            SteamInitialized = false;
            Console.WriteLine($"Steam init FAILED: {ex.Message}");
            return false;
        }
    }

    public void RunCallbacks()
    {
        if (!SteamInitialized) return;
        try
        {
            SteamClient.RunCallbacks();
        }
        catch (Exception)
        {
            HandleSteamDisconnect();
        }
    }

    public void Shutdown()
    {
        if (SteamInitialized)
        {
            // Close all P2P sessions
            foreach (var peerId in _connectedPeers)
            {
                SteamNetworking.CloseP2PSessionWithUser(peerId);
            }
            _connectedPeers.Clear();

            SteamClient.Shutdown();
            SteamInitialized = false;
        }
    }

    // --- Host/Join (Direct P2P, no lobbies) ---

    public void HostGame()
    {
        _state.IsHost = true;
        _state.Phase = GamePhase.InLobby;
        _hostSteamId = _state.LocalSteamId;
        _nextColorIndex = 0;
        AddPlayer(_state.LocalSteamId, LocalPlayerName, _nextColorIndex++);
        Console.WriteLine($"Hosting game. SteamId={_state.LocalSteamId}");
    }

    public void DirectConnect(ulong hostSteamId)
    {
        Console.WriteLine($"Direct connecting to host {hostSteamId}...");
        _hostSteamId = hostSteamId;
        _state.IsHost = false;
        _state.Phase = GamePhase.InLobby;

        // Send join request to host via P2P
        string myName = LocalPlayerName;
        var data = PacketSerializer.WritePlayerJoined(_state.LocalSteamId, myName, 0);
        SteamNetworking.SendP2PPacket(hostSteamId, data, data.Length, 0, P2PSend.Reliable);
        Console.WriteLine($"Sent PlayerJoined to host {hostSteamId}");
    }

    public int GetPlayerCount()
    {
        return _state.Players.Count;
    }

    // --- P2P ---

    private void OnP2PSessionRequest(SteamId id)
    {
        Console.WriteLine($"P2P session request from {id}");
        SteamNetworking.AcceptP2PSessionWithUser(id);
        _connectedPeers.Add(id);
    }

    public void Update(float dt)
    {
        if (!SteamInitialized) return;

        try
        {
            while (SteamNetworking.IsP2PPacketAvailable(0))
            {
                var packet = SteamNetworking.ReadP2PPacket(0);
                if (packet.HasValue)
                {
                    ProcessPacket(packet.Value.Data, packet.Value.SteamId);
                }
            }
        }
        catch (Exception)
        {
            HandleSteamDisconnect();
            return;
        }

        if (_state.IsHost && _state.Phase == GamePhase.Playing)
        {
            HostTick(dt);
        }
    }

    private void ProcessPacket(byte[] data, SteamId sender)
    {
        if (data.Length == 0) return;

        using var ms = new MemoryStream(data);
        using var r = new BinaryReader(ms);

        var msgType = (MessageType)r.ReadByte();

        switch (msgType)
        {
            case MessageType.PlayerJoined:
                HandlePlayerJoined(r, sender);
                break;
            case MessageType.PlayerLeft:
                HandlePlayerLeft(r);
                break;
            case MessageType.PlayerMove:
                HandlePlayerMove(r);
                break;
            case MessageType.GatherRequest:
                HandleGatherRequest(r);
                break;
            case MessageType.GatherResult:
                break;
            case MessageType.WorldState:
                HandleWorldState(r);
                break;
            case MessageType.ToolUpgradeRequest:
                HandleToolUpgradeRequest(r);
                break;
            case MessageType.ToolUpgraded:
                HandleToolUpgraded(r);
                break;
            case MessageType.GameWon:
                HandleGameWon(r);
                break;
        }
    }

    // --- Message Handlers ---

    private void HandlePlayerJoined(BinaryReader r, SteamId sender)
    {
        var (steamId, name, receivedColorIndex) = PacketSerializer.ReadPlayerJoined(r);
        Console.WriteLine($"HandlePlayerJoined: {name} ({steamId})");

        _connectedPeers.Add(steamId);

        if (_state.IsHost)
        {
            int colorIndex = _nextColorIndex++;
            AddPlayer(steamId, name, colorIndex);

            // Tell everyone about the new player
            var joinData = PacketSerializer.WritePlayerJoined(steamId, name, colorIndex);
            SendToAllExcept(joinData, steamId);

            // Tell the new player about existing players
            foreach (var p in _state.Players.Values)
            {
                if (p.SteamId == steamId) continue;
                int ci = Array.IndexOf(Player.PlayerColors, p.Color);
                if (ci < 0) ci = 0;
                var existingData = PacketSerializer.WritePlayerJoined(p.SteamId, p.Name, ci);
                SendTo(sender, existingData);
            }
        }
        else
        {
            AddPlayer(steamId, name, receivedColorIndex);
        }
    }

    private void HandlePlayerLeft(BinaryReader r)
    {
        var steamId = PacketSerializer.ReadPlayerLeft(r);
        _state.Players.Remove(steamId);
        _connectedPeers.Remove(steamId);
    }

    private void HandlePlayerMove(BinaryReader r)
    {
        var (steamId, position) = PacketSerializer.ReadPlayerMove(r);

        if (_state.IsHost)
        {
            if (_state.Players.TryGetValue(steamId, out var player))
            {
                player.Position = position;
                if (player.GatheringNodeId.HasValue)
                {
                    player.GatheringNodeId = null;
                    player.GatherProgress = 0;
                }
            }
        }
        else
        {
            if (_state.Players.TryGetValue(steamId, out var player) && steamId != _state.LocalSteamId)
            {
                player.Position = position;
            }
        }
    }

    private void HandleGatherRequest(BinaryReader r)
    {
        if (!_state.IsHost) return;

        var (steamId, nodeId) = PacketSerializer.ReadGatherRequest(r);
        if (!_state.Players.TryGetValue(steamId, out var player)) return;
        if (nodeId < 0 || nodeId >= _state.ResourceNodes.Count) return;

        var node = _state.ResourceNodes[nodeId];
        if (node.IsDepleted) return;

        float dist = Vector2.Distance(player.Position, node.Position);
        if (dist > GameConstants.GatherRange + 10f) return;

        player.GatheringNodeId = nodeId;
        player.GatherProgress = 0;
    }

    private void HandleWorldState(BinaryReader r)
    {
        if (_state.IsHost) return;
        PacketSerializer.ReadWorldState(r, _state);
    }

    private void HandleToolUpgradeRequest(BinaryReader r)
    {
        if (!_state.IsHost) return;

        var (steamId, tool) = PacketSerializer.ReadToolUpgradeRequest(r);
        if (!_state.Players.TryGetValue(steamId, out var player)) return;

        TryUpgrade(player, tool);
    }

    private void HandleToolUpgraded(BinaryReader r)
    {
        var (steamId, tool, newLevel) = PacketSerializer.ReadToolUpgraded(r);
        if (_state.Players.TryGetValue(steamId, out var player))
        {
            player.ToolLevels[tool] = newLevel;
        }
    }

    private void HandleGameWon(BinaryReader r)
    {
        var winnerId = PacketSerializer.ReadGameWon(r);
        _state.WinnerId = winnerId;
        _state.Phase = GamePhase.Victory;
        _state.VictoryTimer = 0;
    }

    // --- Host Logic ---

    private void HostTick(float dt)
    {
        foreach (var player in _state.Players.Values)
        {
            if (!player.GatheringNodeId.HasValue) continue;

            int nodeId = player.GatheringNodeId.Value;
            if (nodeId < 0 || nodeId >= _state.ResourceNodes.Count)
            {
                player.GatheringNodeId = null;
                player.GatherProgress = 0;
                continue;
            }

            var node = _state.ResourceNodes[nodeId];
            if (node.IsDepleted)
            {
                player.GatheringNodeId = null;
                player.GatherProgress = 0;
                continue;
            }

            float dist = Vector2.Distance(player.Position, node.Position);
            if (dist > GameConstants.GatherRange + 10f)
            {
                player.GatheringNodeId = null;
                player.GatherProgress = 0;
                continue;
            }

            float gatherTime = GetGatherTime(node.Type, player);
            if (gatherTime <= 0) gatherTime = 0.1f;

            player.GatherProgress += dt / gatherTime;

            if (player.GatherProgress >= 1.0f)
            {
                player.GatherProgress = 0;
                node.RemainingAmount--;
                player.Inventory[node.Type]++;

                if (node.IsDepleted)
                {
                    node.RespawnTimer = GameConstants.RespawnTime;
                    player.GatheringNodeId = null;
                }
            }
        }

        foreach (var node in _state.ResourceNodes)
        {
            if (node.IsDepleted && node.RespawnTimer > 0)
            {
                node.RespawnTimer -= dt;
                if (node.RespawnTimer <= 0)
                {
                    node.RespawnTimer = 0;
                    node.RemainingAmount = node.MaxAmount;
                }
            }
        }

        foreach (var player in _state.Players.Values)
        {
            if (player.ToolLevels[ToolType.Axe] >= 3 &&
                player.ToolLevels[ToolType.Pickaxe] >= 3 &&
                player.ToolLevels[ToolType.GoldPick] >= 3)
            {
                _state.WinnerId = player.SteamId;
                _state.Phase = GamePhase.Victory;
                _state.VictoryTimer = 0;

                var wonData = PacketSerializer.WriteGameWon(player.SteamId);
                SendToAll(wonData);
                return;
            }
        }

        _networkTickTimer += dt;
        if (_networkTickTimer >= GameConstants.NetworkTickInterval)
        {
            _networkTickTimer -= GameConstants.NetworkTickInterval;
            BroadcastWorldState();
        }
    }

    private float GetGatherTime(ResourceType type, Player player)
    {
        float baseTime = type switch
        {
            ResourceType.Wood => GameConstants.GatherTimeWood,
            ResourceType.Iron => GameConstants.GatherTimeIron,
            ResourceType.Gold => GameConstants.GatherTimeGold,
            _ => 3.0f
        };

        var toolType = type switch
        {
            ResourceType.Wood => ToolType.Axe,
            ResourceType.Iron => ToolType.Pickaxe,
            ResourceType.Gold => ToolType.GoldPick,
            _ => ToolType.Axe
        };

        int level = player.ToolLevels[toolType];
        float multiplier = level switch
        {
            1 => 1.0f,
            2 => 0.7f,
            3 => 0.4f,
            _ => 1.0f
        };

        return baseTime * multiplier;
    }

    private void TryUpgrade(Player player, ToolType tool)
    {
        var cost = UpgradePanel.GetUpgradeCost(tool, player.ToolLevels[tool]);
        if (cost == null) return;

        var c = cost.Value;
        if (player.Inventory[ResourceType.Wood] < c.Wood ||
            player.Inventory[ResourceType.Iron] < c.Iron ||
            player.Inventory[ResourceType.Gold] < c.Gold)
            return;

        player.Inventory[ResourceType.Wood] -= c.Wood;
        player.Inventory[ResourceType.Iron] -= c.Iron;
        player.Inventory[ResourceType.Gold] -= c.Gold;

        player.ToolLevels[tool]++;

        var data = PacketSerializer.WriteToolUpgraded(player.SteamId, tool, player.ToolLevels[tool]);
        SendToAll(data);
    }

    // --- Sending ---

    public void SendPlayerMove(Vector2 position)
    {
        if (!SteamInitialized) return;
        var data = PacketSerializer.WritePlayerMove(_state.LocalSteamId, position);

        if (!_state.IsHost)
        {
            SendToHost(data, P2PSend.UnreliableNoDelay);
        }
    }

    public void SendGatherRequest(int nodeId)
    {
        if (!SteamInitialized) return;
        var data = PacketSerializer.WriteGatherRequest(_state.LocalSteamId, nodeId);

        if (_state.IsHost)
        {
            if (_state.Players.TryGetValue(_state.LocalSteamId, out var player))
            {
                if (nodeId >= 0 && nodeId < _state.ResourceNodes.Count)
                {
                    var node = _state.ResourceNodes[nodeId];
                    if (!node.IsDepleted)
                    {
                        float dist = Vector2.Distance(player.Position, node.Position);
                        if (dist <= GameConstants.GatherRange + 10f)
                        {
                            player.GatheringNodeId = nodeId;
                            player.GatherProgress = 0;
                        }
                    }
                }
            }
        }
        else
        {
            SendToHost(data, P2PSend.Reliable);
        }
    }

    public void SendToolUpgradeRequest(ToolType tool)
    {
        if (!SteamInitialized) return;

        if (_state.IsHost)
        {
            if (_state.Players.TryGetValue(_state.LocalSteamId, out var player))
            {
                TryUpgrade(player, tool);
            }
        }
        else
        {
            var data = PacketSerializer.WriteToolUpgradeRequest(_state.LocalSteamId, tool);
            SendToHost(data, P2PSend.Reliable);
        }
    }

    public void StartGame()
    {
        if (!_state.IsHost) return;

        _state.GenerateWorld();

        var playerList = _state.Players.Values.ToList();
        var spawnPositions = new Vector2[]
        {
            new(GameConstants.MapWidth / 2f - 100, GameConstants.MapHeight / 2f - 100),
            new(GameConstants.MapWidth / 2f + 100, GameConstants.MapHeight / 2f - 100),
            new(GameConstants.MapWidth / 2f - 100, GameConstants.MapHeight / 2f + 100),
            new(GameConstants.MapWidth / 2f + 100, GameConstants.MapHeight / 2f + 100),
        };

        for (int i = 0; i < playerList.Count; i++)
        {
            playerList[i].Position = spawnPositions[i % spawnPositions.Length];
        }

        _state.Phase = GamePhase.Playing;
        BroadcastWorldState();
    }

    private void BroadcastWorldState()
    {
        var data = PacketSerializer.WriteWorldState(_state);
        SendToAll(data);
    }

    private void SendToAll(byte[] data)
    {
        if (!SteamInitialized) return;
        try
        {
            foreach (var peerId in _connectedPeers)
            {
                if (peerId == _state.LocalSteamId) continue;
                SteamNetworking.SendP2PPacket(peerId, data, data.Length, 0, P2PSend.Reliable);
            }
        }
        catch (Exception) { HandleSteamDisconnect(); }
    }

    private void SendToAllExcept(byte[] data, ulong exceptId)
    {
        if (!SteamInitialized) return;
        try
        {
            foreach (var peerId in _connectedPeers)
            {
                if (peerId == _state.LocalSteamId || peerId == exceptId) continue;
                SteamNetworking.SendP2PPacket(peerId, data, data.Length, 0, P2PSend.Reliable);
            }
        }
        catch (Exception) { HandleSteamDisconnect(); }
    }

    private void SendTo(SteamId target, byte[] data)
    {
        if (!SteamInitialized) return;
        try { SteamNetworking.SendP2PPacket(target, data, data.Length, 0, P2PSend.Reliable); }
        catch (Exception) { HandleSteamDisconnect(); }
    }

    private void SendToHost(byte[] data, P2PSend sendType)
    {
        if (!SteamInitialized) return;
        try { SteamNetworking.SendP2PPacket(_hostSteamId, data, data.Length, 0, sendType); }
        catch (Exception) { HandleSteamDisconnect(); }
    }

    private void HandleSteamDisconnect()
    {
        Console.WriteLine("Steam disconnected! Returning to main menu.");
        SteamInitialized = false;
        SteamWasDisconnected = true;
        _connectedPeers.Clear();
        _state.Players.Clear();
        _state.Phase = GamePhase.MainMenu;
        _state.IsHost = false;
    }

    // --- Helpers ---

    private void AddPlayer(ulong steamId, string name, int colorIndex)
    {
        if (_state.Players.ContainsKey(steamId)) return;

        var color = Player.PlayerColors[colorIndex % Player.PlayerColors.Length];
        _state.Players[steamId] = new Player
        {
            SteamId = steamId,
            Name = name,
            Color = color,
            Position = new Vector2(GameConstants.MapWidth / 2f, GameConstants.MapHeight / 2f)
        };
        Console.WriteLine($"Player added: {name} ({steamId}) color={colorIndex}");
    }
}
