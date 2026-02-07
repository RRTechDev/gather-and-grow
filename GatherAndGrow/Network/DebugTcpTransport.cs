#if DEBUG
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace GatherAndGrow.Network;

public class DebugTcpTransport : IDisposable
{
    public const ulong FakeHostId = 1000001;
    public const ulong FakeClientId = 1000002;
    public const int Port = 7777;

    private TcpListener? _listener;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private readonly ConcurrentQueue<(ulong SenderId, byte[] Data)> _inbox = new();
    private readonly ulong _localId;
    private volatile bool _disposed;

    public DebugTcpTransport(bool asHost)
    {
        _localId = asHost ? FakeHostId : FakeClientId;
    }

    public void StartHost()
    {
        _listener = new TcpListener(IPAddress.Loopback, Port);
        _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _listener.Start();
        Console.WriteLine($"[DebugTcp] Host listening on port {Port}");

        Task.Run(async () =>
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync();
                _client = client;
                _stream = client.GetStream();
                Console.WriteLine("[DebugTcp] Client connected");
                await ReadLoopAsync(_stream, FakeClientId);
            }
            catch (Exception ex) when (!_disposed)
            {
                Console.WriteLine($"[DebugTcp] Host accept error: {ex.Message}");
            }
        });
    }

    public void ConnectToHost()
    {
        _client = new TcpClient();
        _client.Connect(IPAddress.Loopback, Port);
        _stream = _client.GetStream();
        Console.WriteLine("[DebugTcp] Connected to host");

        Task.Run(async () =>
        {
            try
            {
                await ReadLoopAsync(_stream, FakeHostId);
            }
            catch (Exception ex) when (!_disposed)
            {
                Console.WriteLine($"[DebugTcp] Client read error: {ex.Message}");
            }
        });
    }

    public void Send(byte[] payload)
    {
        var stream = _stream;
        if (stream == null) return;

        try
        {
            // Frame: [4-byte length][8-byte senderId][payload]
            int frameLen = 8 + payload.Length;
            var header = new byte[4 + 8];
            BitConverter.GetBytes(frameLen).CopyTo(header, 0);
            BitConverter.GetBytes(_localId).CopyTo(header, 4);

            lock (stream)
            {
                stream.Write(header, 0, header.Length);
                stream.Write(payload, 0, payload.Length);
                stream.Flush();
            }
        }
        catch (Exception ex) when (!_disposed)
        {
            Console.WriteLine($"[DebugTcp] Send error: {ex.Message}");
        }
    }

    public bool TryDequeue(out ulong senderId, out byte[] data)
    {
        if (_inbox.TryDequeue(out var item))
        {
            senderId = item.SenderId;
            data = item.Data;
            return true;
        }
        senderId = 0;
        data = Array.Empty<byte>();
        return false;
    }

    public bool IsClientConnected => _stream != null;

    private async Task ReadLoopAsync(NetworkStream stream, ulong peerId)
    {
        var headerBuf = new byte[4 + 8]; // length + senderId

        while (!_disposed)
        {
            // Read frame header
            int read = 0;
            while (read < headerBuf.Length)
            {
                int n = await stream.ReadAsync(headerBuf, read, headerBuf.Length - read);
                if (n == 0) return; // connection closed
                read += n;
            }

            int frameLen = BitConverter.ToInt32(headerBuf, 0);
            ulong senderId = BitConverter.ToUInt64(headerBuf, 4);

            // Read payload (frameLen includes the 8-byte senderId we already read)
            int payloadLen = frameLen - 8;
            if (payloadLen <= 0) continue;

            var payload = new byte[payloadLen];
            read = 0;
            while (read < payloadLen)
            {
                int n = await stream.ReadAsync(payload, read, payloadLen - read);
                if (n == 0) return;
                read += n;
            }

            _inbox.Enqueue((senderId, payload));
        }
    }

    public void Dispose()
    {
        _disposed = true;
        try { _stream?.Close(); } catch { }
        try { _client?.Close(); } catch { }
        try { _listener?.Stop(); } catch { }
    }
}
#endif
