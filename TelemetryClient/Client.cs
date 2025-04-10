using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

class TelemetryClient
{
    static void Main(string[] args)
    {
        // Generate a unique 6-character flight ID for this client
        // This ID is sent to the server so the server can track each flight separately
        string id = Guid.NewGuid().ToString().Substring(0, 6);

        // STEP 1: Determine the server IP address to connect to
        // Check if the server IP is passed as a command-line argument
        string serverIp = "127.0.0.1"; // Default value for local testing
        if (args.Length >= 1 && !string.IsNullOrWhiteSpace(args[0]))
        {
            serverIp = args[0];
            Console.WriteLine($"Using provided server IP: {serverIp}");
        }
        else
        {
            // If not provided as argument, ask the user to input server IP
            Console.Write("Enter server IP (default 127.0.0.1): ");
            string ipInput = Console.ReadLine();
            serverIp = string.IsNullOrWhiteSpace(ipInput) ? "127.0.0.1" : ipInput;
        }

        // STEP 2: Determine the correct absolute path to the data folder
        // Dynamically go up 3 folders from bin/Release/net8.0/ to the project root
        string projectRoot = Directory.GetParent(AppContext.BaseDirectory).Parent.Parent.Parent.FullName;

        // Combine the project root with the data folder path
        string dataFolderPath = Path.Combine(projectRoot, "data");

        // Check if the data folder exists
        if (!Directory.Exists(dataFolderPath))
        {
            Console.WriteLine($"Data folder not found at: {dataFolderPath}");
            return;
        }

        // Look for all .txt telemetry data files inside the 'data' folder
        string[] files = Directory.GetFiles(dataFolderPath, "*.txt");
        if (files.Length == 0)
        {
            Console.WriteLine("No telemetry files found in 'data' folder.");
            return;
        }

        string selectedFile;

        // STEP 3: Determine which telemetry data file to send
        // If a file name is passed as a command-line argument, use it
        if (args.Length >= 2 && !string.IsNullOrWhiteSpace(args[1]))
        {
            selectedFile = Path.Combine(dataFolderPath, args[1]);
            if (!File.Exists(selectedFile))
            {
                Console.WriteLine($"File '{args[1]}' not found in data folder.");
                return;
            }

            Console.WriteLine("Using provided file: " + Path.GetFileName(selectedFile));
        }
        else
        {
            // If no file is provided, use a default hardcoded file
            selectedFile = Path.Combine(dataFolderPath, "katl-kefd-B737-700.txt");
            Console.WriteLine("Default file selected: " + Path.GetFileName(selectedFile));
        }

        // STEP 4: Connect to the server and send telemetry data
        Console.WriteLine($"Client {id} starting...");
        Console.WriteLine($"Connecting to server at {serverIp}...");

        // Establish a TCP connection to the server on port 5000
        using TcpClient client = new TcpClient(serverIp, 5000);
        Console.WriteLine(" Connected to server.");

        // Setup the stream for sending data to the server
        using NetworkStream stream = client.GetStream();
        using StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

        // Send the unique flight ID to the server
        writer.WriteLine(id);

        // Send the name of the data file being used to the server
        writer.WriteLine(Path.GetFileName(selectedFile));

        Console.WriteLine($" Sending data from {Path.GetFileName(selectedFile)}");

        // Read the data file line by line and send fuel readings to the server
        foreach (string line in File.ReadLines(selectedFile))
        {
            // Skip header lines or empty lines
            if (line.StartsWith("FUEL TOTAL") || string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(',');
            if (parts.Length < 2) continue;

            string timestamp = parts[0].Trim(); // Extract timestamp
            string fuel = parts[1].Trim();      // Extract fuel quantity

            // Send the timestamp and fuel reading to the server
            writer.WriteLine($"{timestamp},{fuel}");

            // Display the sent data in the client console
            Console.WriteLine($"Sent: {timestamp}, {fuel}");

            // Introduce a small delay to simulate real-time telemetry data sending
            Thread.Sleep(100);
        }

        // Inform that all data has been sent
        Console.WriteLine($" Client {id} finished sending data.");
    }
}
