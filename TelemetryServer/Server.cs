using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;

class TelemetryServer
{
    // Stores total fuel used per flight ID
    static ConcurrentDictionary<string, double> totalFuelUsed = new();

    // Stores how many fuel data points were received per flight ID
    static ConcurrentDictionary<string, int> timePoints = new();

    // Stores the final average fuel usage per flight ID
    static ConcurrentDictionary<string, double> finalAverages = new();

    static void Main()
    {
        // Set up a TCP listener on port 5000 to accept incoming client connections
        TcpListener listener = new TcpListener(IPAddress.Any, 5000);
        listener.Start();
        Console.WriteLine($"Server started on port 5000 at {DateTime.Now}");

        while (true)
        {
            // Wait for a client to connect
            TcpClient client = listener.AcceptTcpClient();
            Console.WriteLine($" New client connected from {((IPEndPoint)client.Client.RemoteEndPoint).Address}");

            // Start a new thread to handle each connected client independently
            Thread t = new Thread(() => HandleClient(client));
            t.Start();
        }
    }

    // Handles an individual client connection (one flight)
    static void HandleClient(TcpClient client)
    {
        try
        {
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = new StreamReader(stream);

            // First message received is the flight's unique ID
            string planeId = reader.ReadLine();
            string? line;
            double prevFuel = -1; // Used to calculate fuel used per reading

            // Read each line sent by the client
            while ((line = reader.ReadLine()) != null)
            {
                var parts = line.Split(',');
                if (parts.Length < 2) continue;

                string timestamp = parts[0].Trim(); // Time of reading
                if (!double.TryParse(parts[1].Trim(), out double fuel)) continue; // Fuel quantity

                // Log received data
                Console.WriteLine($"Received from {planeId}: {timestamp}, {fuel}");

                // Calculate fuel used from the difference between previous and current fuel
                if (prevFuel != -1)
                    totalFuelUsed.AddOrUpdate(planeId, prevFuel - fuel, (k, v) => v + (prevFuel - fuel));

                prevFuel = fuel;

                // Keep track of how many readings were received
                timePoints.AddOrUpdate(planeId, 1, (k, v) => v + 1);
            }

            // After client disconnects, calculate and log the average fuel usage
            totalFuelUsed.TryGetValue(planeId, out double totalFuel);
            timePoints.TryGetValue(planeId, out int count);

            if (count > 0)
            {
                double avg = totalFuel / count;
                finalAverages[planeId] = avg;

                Console.WriteLine($"Flight {planeId} completed. Lines received: {count}. Avg fuel usage: {avg:F4}");

                // Append the result to a flight log file
                string logEntry = $"{DateTime.Now}, {planeId}, {count} points, {avg:F4}";
                File.AppendAllText("flight_log.txt", logEntry + Environment.NewLine);
            }
            else
            {
                Console.WriteLine($"Flight {planeId} ended, but no fuel data was received.");
            }

            // Confirm client has disconnected
            Console.WriteLine($" Flight {planeId} has disconnected.\n");
        }
        catch (Exception ex)
        {
            // Log any exceptions (e.g., unexpected disconnects)
            Console.WriteLine($" Error handling client: {ex.Message}\n");
        }
    }
}
