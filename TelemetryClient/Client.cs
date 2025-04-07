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

        // Prompt the user to enter the server's IP address
        Console.Write("Enter server IP (default 127.0.0.1): ");
        string ipInput = Console.ReadLine();
        string serverIp = string.IsNullOrWhiteSpace(ipInput) ? "127.0.0.1" : ipInput;

        // Look for all .txt telemetry files in the "data" folder
        string[] files = Directory.GetFiles("data", "*.txt");
        if (files.Length == 0)
        {
            Console.WriteLine("No telemetry files found in 'data' folder.");
            return;
        }

        //// === RANDOM FILE SELECTION ===
        //Random rand = new Random();
        //string selectedFile = files[rand.Next(files.Length)];
        //Console.WriteLine("Randomly selected: " + Path.GetFileName(selectedFile));

        /*
        // === FILE SELECTION MENU ===

        Console.WriteLine("Available flight files:");
        for (int i = 0; i < files.Length; i++)
            Console.WriteLine($"{i + 1}: {Path.GetFileName(files[i])}");

        Console.Write("Choose a file number (or press ENTER for random): ");
        string input = Console.ReadLine();

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
        */

        /*
        // === HARDCODED FILE SELECTION ===
        // Uncomment this to force the client to always use a specific file.

        string selectedFile = Path.Combine("data", "katl-kefd-B737-700.txt");
        Console.WriteLine("Hardcoded file selected: " + Path.GetFileName(selectedFile));
        */

        string selectedFile = Path.Combine("data", "katl-kefd-B737-700.txt");
        Console.WriteLine("Hardcoded file selected: " + Path.GetFileName(selectedFile));

        // Display basic connection info
        Console.WriteLine($"Client {id} starting...");
        Console.WriteLine($"Connecting to server at {serverIp}...");

        // Connect to the server on port 5000
        using TcpClient client = new TcpClient(serverIp, 5000);
        Console.WriteLine(" Connected to server.");

        // Set up network stream and writer for sending data
        using NetworkStream stream = client.GetStream();
        using StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

        // Send the unique client ID to the server
        writer.WriteLine(id);

        Console.WriteLine($" Sending data from {Path.GetFileName(selectedFile)}");

        // Read the selected file line by line and send fuel data
        foreach (string line in File.ReadLines(selectedFile))
        {
            // Skip header lines or empty lines
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

        Console.WriteLine($" Client {id} finished sending data.");
    }
}
