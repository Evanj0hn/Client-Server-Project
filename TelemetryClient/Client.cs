using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

class TelemetryClient
{
    static void Main(string[] args)
    {
        string id = Guid.NewGuid().ToString().Substring(0, 6); // Unique ID

        // Ask user to enter server IP
        Console.Write("Enter server IP (default 127.0.0.1): ");
        string ipInput = Console.ReadLine();
        string serverIp = string.IsNullOrWhiteSpace(ipInput) ? "127.0.0.1" : ipInput;

        // Let user pick from available files or random
        string[] files = Directory.GetFiles("data", "*.txt");
        if (files.Length == 0)
        {
            Console.WriteLine("No telemetry files found in 'data' folder.");
            return;
        }

        Console.WriteLine("Available flight files:");
        for (int i = 0; i < files.Length; i++)
            Console.WriteLine($"{i + 1}: {Path.GetFileName(files[i])}");

        Console.Write("Choose a file number (or press ENTER for random): ");
        string input = Console.ReadLine();

        string selectedFile;
        if (int.TryParse(input, out int choice) && choice > 0 && choice <= files.Length)
        {
            selectedFile = files[choice - 1];
        }
        else
        {
            Random rand = new Random();
            selectedFile = files[rand.Next(files.Length)];
            Console.WriteLine("Randomly selected: " + Path.GetFileName(selectedFile));
        }

        Console.WriteLine($"Client {id} started. Sending data from {Path.GetFileName(selectedFile)}");

        using TcpClient client = new TcpClient(serverIp, 5000);
        using NetworkStream stream = client.GetStream();
        using StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

        writer.WriteLine(id); // Send plane ID

        foreach (string line in File.ReadLines(selectedFile))
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
