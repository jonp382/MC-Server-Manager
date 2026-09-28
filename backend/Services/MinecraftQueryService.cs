namespace MinecraftServerManager.Services;

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

public class ServerStatusResult
{
    public bool IsOnline { get; set; }
    public string Motd { get; set; } = string.Empty;
    public int OnlinePlayers { get; set; }
    public int MaxPlayers { get; set; }
}

public class MinecraftQueryService
{
    private readonly IConfiguration _config;

    public MinecraftQueryService(IConfiguration config)
    {
        _config = config;
    }

    public async Task<ServerStatusResult> GetServerStatusAsync(int timeoutMs = 2000)
    {
        string host = _config["Minecraft:ServerIp"] ?? "127.0.0.1";
        int port = int.Parse(_config["Minecraft:QueryPort"] ?? "25565");

        var result = new ServerStatusResult { IsOnline = false };

        try
        {
            using var udpClient = new UdpClient();
            udpClient.Client.ReceiveTimeout = timeoutMs;
            udpClient.Client.SendTimeout = timeoutMs;
            udpClient.Connect(host, port);

            // 1. Send Handshake Request (Packet Header: 0xFE, 0xFD, Type: 0x09, SessionID: 1)
            byte[] handshakeRequest = new byte[] { 0xFE, 0xFD, 0x09, 0x00, 0x00, 0x00, 0x01 };
            await udpClient.SendAsync(handshakeRequest, handshakeRequest.Length);

            var receiveTask = udpClient.ReceiveAsync();
            if (await Task.WhenAny(receiveTask, Task.Delay(timeoutMs)) != receiveTask)
            {
                Console.WriteLine($"[INFO] [QueryService] Timeout waiting for handshake response");
                return result; // Timed out (Server Offline)
            }

            byte[] handshakeResponse = receiveTask.Result.Buffer;
            if (handshakeResponse.Length < 5) return result;

            // Extract Challenge Token string from response
            string challengeTokenStr = System.Text.Encoding.ASCII.GetString(handshakeResponse, 5, handshakeResponse.Length - 6);
            if (!int.TryParse(challengeTokenStr, out int challengeToken)) return result;

            // 2. Send Basic Stat Request with Challenge Token
            byte[] tokenBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(challengeToken));
            byte[] statRequest = new byte[15];
            statRequest[0] = 0xFE;
            statRequest[1] = 0xFD;
            statRequest[2] = 0x00; // Type: Stat
            statRequest[3] = 0x00; statRequest[4] = 0x00; statRequest[5] = 0x00; statRequest[6] = 0x01; // Session ID
            Array.Copy(tokenBytes, 0, statRequest, 7, 4);

            await udpClient.SendAsync(statRequest, statRequest.Length);

            var statTask = udpClient.ReceiveAsync();
            if (await Task.WhenAny(statTask, Task.Delay(timeoutMs)) != statTask)
            {
                Console.WriteLine($"[INFO] [QueryService] Timeout waiting for server response");
                return result; // Timed out
            }

            byte[] statResponse = statTask.Result.Buffer;

            // Parse response fields (MOTD, Online Players, Max Players)
            // Structure after 5-byte header: MOTD\0 Gametype\0 Map\0 NumPlayers\0 MaxPlayers\0
            string payload = System.Text.Encoding.ASCII.GetString(statResponse, 5, statResponse.Length - 5);
            string[] parts = payload.Split('\0');

            if (parts.Length >= 5)
            {
                result.IsOnline = true;
                result.Motd = parts[0];
                int.TryParse(parts[3], out int online);
                int.TryParse(parts[4], out int max);
                result.OnlinePlayers = online;
                result.MaxPlayers = max;
            }
            else
            {
                result.IsOnline = true; // Fallback: responded to ping even if parsing failed
            }

            Console.WriteLine($"[INFO] [QueryService] Response: {result.IsOnline}");
            return result;
        }
        catch
        {
            // Socket errors, connection refused, or timeouts mean the server is offline
            Console.WriteLine($"[ERROR] [QueryService] Server is offline");
            return result;
        }
    }
}