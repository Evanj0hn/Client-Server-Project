using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;

class TelemetryServer
{
    static ConcurrentDictionary<string, double> totalFuelUsed = new();
    static ConcurrentDictionary<string, int> timePoints = new();
    static ConcurrentDictionary<string, double> finalAverages = new();

    static void Main()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, 5000);
        listener.Start();
        Console.WriteLine($"Server started on port 5000 at {DateTime.Now}");

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            Thread t = new Thread(() => HandleClient(client));
            t.Start();
        }
    }

    static void HandleClient(TcpClient client)
    {
        using NetworkStream stream = client.GetStream();
        using StreamReader reader = new StreamReader(stream);

        string planeId = reader.ReadLine(); // Unique flight ID
        string? line;
        double prevFuel = -1;

        while ((line = reader.ReadLine()) != null)
        {
            var parts = line.Split(',');
            if (parts.Length < 2) continue;

            string timestamp = parts[0].Trim();
            if (!double.TryParse(parts[1].Trim(), out double fuel)) continue;

            Console.WriteLine($"Received from {planeId}: {timestamp}, {fuel}");

            if (prevFuel != -1)
                totalFuelUsed.AddOrUpdate(planeId, prevFuel - fuel, (k, v) => v + (prevFuel - fuel));

            prevFuel = fuel;
            timePoints.AddOrUpdate(planeId, 1, (k, v) => v + 1);
        }

        totalFuelUsed.TryGetValue(planeId, out double totalFuel);
        timePoints.TryGetValue(planeId, out int count);

        if (count > 0)
        {
            double avg = totalFuel / count;
            finalAverages[planeId] = avg;

            Console.WriteLine($"Flight {planeId} completed. Lines received: {count}. Avg fuel usage: {avg:F4}");

            string logEntry = $"{DateTime.Now}, {planeId}, {count} points, {avg:F4}";
            File.AppendAllText("flight_log.txt", logEntry + Environment.NewLine);
        }
        else
        {
            Console.WriteLine($"Flight {planeId} ended, but no fuel data was received.");
        }

        Console.WriteLine($"Flight {planeId} has disconnected.\n");
    }
}
