using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class TelemetryServer
{
    static ConcurrentDictionary<string, double> totalFuelUsed = new();
    static ConcurrentDictionary<string, int> timePoints = new();

    static void Main()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, 5000);
        listener.Start();
        Console.WriteLine("Server started on port 5000...");

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
        string id = reader.ReadLine(); // First line is the Plane ID
        string? line;

        double previousFuel = -1;

        while ((line = reader.ReadLine()) != null)
        {
            var parts = line.Split(',');
            if (parts.Length != 2) continue;

            double time = double.Parse(parts[0]);
            double fuel = double.Parse(parts[1]);

            if (previousFuel != -1)
                totalFuelUsed.AddOrUpdate(id, previousFuel - fuel, (k, v) => v + (previousFuel - fuel));

            previousFuel = fuel;
            timePoints.AddOrUpdate(id, 1, (k, v) => v + 1);
        }

        double avg = totalFuelUsed[id] / timePoints[id];
        Console.WriteLine($"Flight {id} ended. Avg Fuel Usage: {avg} gallons per point.");
    }
}
