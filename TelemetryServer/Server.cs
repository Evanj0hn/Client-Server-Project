using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;

class TelemetryServer
{
    // Thread-safe dictionary to store total fuel used per flight ID
    static ConcurrentDictionary<string, double> totalFuelUsed = new();

    // Thread-safe dictionary to store number of fuel data points per flight ID
    static ConcurrentDictionary<string, int> timePoints = new();

    // Thread-safe dictionary to store final average fuel usage per flight ID
    static ConcurrentDictionary<string, double> finalAverages = new();

    // Lock object used only for synchronizing writes to flight_log.txt
    static readonly object logFileLock = new();

    static void Main()
    {
        // TCP Listener binds to port 5000 and waits for client connections
        TcpListener listener = new TcpListener(IPAddress.Any, 5000);
        listener.Start();
        Console.WriteLine($"Server started on port 5000 at {DateTime.Now}");

        // Infinite loop to handle multiple incoming client connections
        while (true)
        {
            // Accept a client connection (blocking call)
            TcpClient client = listener.AcceptTcpClient();
            Console.WriteLine($" New client connected from {((IPEndPoint)client.Client.RemoteEndPoint).Address}");

            // Create a separate thread for each client connection
            // This is what allows the server to work in parallel
            Thread t = new Thread(() => HandleClient(client));
            t.Start();

            /*
             * PARALLELISM EXPLANATION:
             * Each incoming client (flight) is handled in its own thread.
             * All threads run independently and simultaneously.
             * There is no blocking between clients because:
             * - Flight data is stored in ConcurrentDictionary (thread-safe collection).
             * - File writes are synchronized using 'lock' (only for flight_log.txt).
             * This can handle multiple client connections at the same time without conflicts.
             */
        }
    }

    // Method to handle a single client (flight) connection
    static void HandleClient(TcpClient client)
    {
        try
        {
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = new StreamReader(stream);

            // Extract client connection details for display
            IPEndPoint remoteEP = (IPEndPoint)client.Client.RemoteEndPoint;
            string ip = remoteEP.Address.ToString();
            int port = remoteEP.Port;

            // First line from client is the unique flight ID
            string planeId = reader.ReadLine();

            // Second line from client is the name of the telemetry data file used
            string dataFileName = reader.ReadLine();

            string? line;
            double prevFuel = -1;

            // Continuously read telemetry data lines from client
            while ((line = reader.ReadLine()) != null)
            {
                var parts = line.Split(',');
                if (parts.Length < 2) continue;

                string timestamp = parts[0].Trim();
                if (!double.TryParse(parts[1].Trim(), out double fuel)) continue;

                // Read the current sequence count for this flight
                timePoints.TryGetValue(planeId, out int seq);
                seq++;

                // Display formatted data received from client
                Console.WriteLine($"[{port,5}] {ip}:{port} | ID: {planeId,-5} | Fuel: {fuel,6:F2} | Seq: {seq,4} | Time: {timestamp}");

                // Calculate fuel used based on previous reading
                if (prevFuel != -1)
                    totalFuelUsed.AddOrUpdate(planeId, prevFuel - fuel, (k, v) => v + (prevFuel - fuel));

                prevFuel = fuel;

                // Update the sequence count (number of points received)
                timePoints.AddOrUpdate(planeId, 1, (k, v) => v + 1);
            }

            // After client disconnects, calculate average fuel usage
            totalFuelUsed.TryGetValue(planeId, out double totalFuel);
            timePoints.TryGetValue(planeId, out int count);

            if (count > 0)
            {
                double avg = totalFuel / count;
                finalAverages[planeId] = avg;

                Console.WriteLine($"Flight {planeId} completed. Lines received: {count}. Avg fuel usage: {avg:F4}");

                // Prepare log entry for flight_log.txt
                // This includes the plane ID, file name, client IP, total points, and average fuel usage
                string logEntry = $"{DateTime.Now}, {planeId}, {dataFileName}, {ip}, {count} points, {avg:F4}";

                // Append the log entry in a thread-safe way using lock
                lock (logFileLock)
                {
                    File.AppendAllText("flight_log.txt", logEntry + Environment.NewLine);
                }
            }
            else
            {
                Console.WriteLine($"Flight {planeId} ended, but no fuel data was received.");
            }

            Console.WriteLine($" Flight {planeId} has disconnected.\n");
        }
        catch (Exception ex)
        {
            // Handle unexpected errors during client handling
            Console.WriteLine($" Error handling client: {ex.Message}\n");
        }
    }
}
