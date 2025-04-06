using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

class TelemetryClient
{
    static void Main(string[] args)
    {
        string id = Guid.NewGuid().ToString().Substring(0, 6); // Unique ID
        string filePath = "katl-kefd-B737-700.txt"; // Change if needed
        string serverIp = "127.0.0.1"; // Change to actual IP for testing

        using TcpClient client = new TcpClient(serverIp, 5000);
        using NetworkStream stream = client.GetStream();
        using StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

        writer.WriteLine(id); // Send Plane ID first

        foreach (string line in File.ReadLines(filePath))
        {
            writer.WriteLine(line);
            Thread.Sleep(500); // Simulate real-time delay
        }

        Console.WriteLine($"Client {id} finished sending data.");
    }
}
