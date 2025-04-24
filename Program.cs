using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var client = new ABXClient("xxxx", 3000);
        await client.StartAsync();
    }
}

public class ABXClient
{
    private readonly string _host;
    private readonly int _port;
    private const int PacketSize = 17;

    public ABXClient(string host, int port)
    {
        _host = host;
        _port = port;
    }

    public async Task StartAsync()
    {
        var packets = new Dictionary<int, byte[]>();
        using (var tcpClient = new TcpClient())
        {
            await tcpClient.ConnectAsync(_host, _port);
            using var stream = tcpClient.GetStream();
            await SendStreamAllRequestAsync(stream);
            packets = await ReceiveAllPacketsAsync(stream);
        }

        var missingSequences = GetMissingSequences(packets);
        Console.WriteLine($"Missing sequences: {string.Join(", ", missingSequences)}");

        foreach (int seq in missingSequences)
        {
            var packet = await RequestResendPacketAsync(seq);
            if (packet != null)
                packets[seq] = packet;
        }

        Console.WriteLine("\nFinal Packet List:");
        foreach (var packet in packets.Values)
        {
            ParseAndPrintPacket(packet);
        }
    }

    private async Task SendStreamAllRequestAsync(NetworkStream stream)
    {
        byte[] payload = new byte[2] { 1, 0 };
        await stream.WriteAsync(payload, 0, payload.Length);
    }

    private async Task<Dictionary<int, byte[]>> ReceiveAllPacketsAsync(NetworkStream stream)
    {
        var packets = new Dictionary<int, byte[]>();
        var buffer = new byte[PacketSize];

        try
        {
            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, PacketSize);
                if (bytesRead != PacketSize)
                    break;

                int sequence = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 13));
                byte[] packetCopy = new byte[PacketSize];
                Buffer.BlockCopy(buffer, 0, packetCopy, 0, PacketSize);
                packets[sequence] = packetCopy;
            }
        }
        catch (IOException)
        {
            Console.WriteLine("Server closed connection after streaming.");
        }

        return packets;
    }

    private List<int> GetMissingSequences(Dictionary<int, byte[]> packets)
    {
        var missing = new List<int>();
        if (packets.Count == 0) return missing;

        int max = 0;
        foreach (var key in packets.Keys)
            if (key > max) max = key;

        for (int i = 1; i < max; i++)
            if (!packets.ContainsKey(i))
                missing.Add(i);

        return missing;
    }

    private async Task<byte[]> RequestResendPacketAsync(int sequence)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_host, _port);
            using var stream = client.GetStream();

            byte[] payload = new byte[2] { 2, (byte)sequence };
            await stream.WriteAsync(payload, 0, payload.Length);

            byte[] buffer = new byte[PacketSize];
            int bytesRead = await stream.ReadAsync(buffer, 0, PacketSize);
            return bytesRead == PacketSize ? buffer : null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get missing packet {sequence}: {ex.Message}");
            return null;
        }
    }

    private void ParseAndPrintPacket(byte[] packet)
    {
        string symbol = Encoding.ASCII.GetString(packet, 0, 4);
        char indicator = (char)packet[4];
        int quantity = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(packet, 5));
        int price = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(packet, 9));
        int sequence = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(packet, 13));

        Console.WriteLine($"Seq: {sequence}, Symbol: {symbol}, Type: {indicator}, Qty: {quantity}, Price: {price}");
        Console.ReadLine();
    }
}
