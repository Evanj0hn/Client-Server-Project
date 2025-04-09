using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

class TelemetryClient
{
    static void Main(string[] args)
    {
        // Generate a unique 6-character flight ID for this client
        string id = Guid.NewGuid().ToString().Substring(0, 6);

        // STEP 1: Determine server IP
        string serverIp = "127.0.0.1";
        if (args.Length >= 1 && !string.IsNullOrWhiteSpace(args[0]))
        {
            serverIp = args[0];
            Console.WriteLine($"Using provided server IP: {serverIp}");
        }
        else
        {
            Console.Write("Enter server IP (default 127.0.0.1): ");
            string ipInput = Console.ReadLine();
            serverIp = string.IsNullOrWhiteSpace(ipInput) ? "127.0.0.1" : ipInput;
        }

        // STEP 2: Determine selected file
        string[] files = Directory.GetFiles("data", "*.txt");
        if (files.Length == 0)
        {
            Console.WriteLine("No telemetry files found in 'data' folder.");
            return;
        }

        string selectedFile;

        if (args.Length >= 2 && !string.IsNullOrWhiteSpace(args[1]))
        {
            selectedFile = Path.Combine("data", args[1]);
            if (!File.Exists(selectedFile))
            {
                Console.WriteLine($"File '{args[1]}' not found in data folder.");
                return;
            }

            Console.WriteLine("Using provided file: " + Path.GetFileName(selectedFile));
        }
        else
        {
            // Default hardcoded or random fallback
            selectedFile = Path.Combine("data", "katl-kefd-B737-700.txt");
            Console.WriteLine("Default file selected: " + Path.GetFileName(selectedFile));
        }

        // STEP 3: Connect and send data
        Console.WriteLine($"Client {id} starting...");
        Console.WriteLine($"Connecting to server at {serverIp}...");

        using TcpClient client = new TcpClient(serverIp, 5000);
        Console.WriteLine(" Connected to server.");

        using NetworkStream stream = client.GetStream();
        using StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

        writer.WriteLine(id); // Send flight ID

        Console.WriteLine($" Sending data from {Path.GetFileName(selectedFile)}");

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

            Thread.Sleep(100); 
        }

        Console.WriteLine($" Client {id} finished sending data.");
    }
}
