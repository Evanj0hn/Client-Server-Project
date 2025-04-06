using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

class TelemetryClient
{
    static void Main(string[] args)
    {
        string id = Guid.NewGuid().ToString().Substring(0, 6); // Unique flight ID
        string fileName = args.Length > 0 ? args[0] : "katl-kefd-B737-700.txt";
        string filePath = Path.Combine("data", fileName);
        string serverIp = args.Length > 1 ? args[1] : "127.0.0.1";

        Console.WriteLine($"Client {id} started. Sending data from {fileName}");

        using TcpClient client = new TcpClient(serverIp, 5000);
        using NetworkStream stream = client.GetStream();
        using StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

        writer.WriteLine(id); // Send flight ID first

        foreach (string line in File.ReadLines(filePath))
        {
            if (line.StartsWith("FUEL TOTAL") || string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(',');
            if (parts.Length < 2) continue;

            string timestamp = parts[0].Trim();
            string fuel = parts[1].Trim();

            writer.WriteLine($"{timestamp},{fuel}");
            Console.WriteLine($"Sent: {timestamp}, {fuel}");
            Thread.Sleep(500); // Simulate delay
        }

        Console.WriteLine($"Client {id} finished sending data.");
    }
}
