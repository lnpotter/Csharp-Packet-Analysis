using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Npgsql;
using SharpPcap;
using PacketDotNet;

namespace NetworkTrafficMonitor
{
    public class Configuration
    {
        public string? ConnectionString { get; set; }
        public string? CaptureDevice { get; set; }
    }

    public class PacketCaptureService
    {
        private static ICaptureDevice? device;
        private static List<string> packetInsertBatch = new List<string>();
        private const int BatchSize = 100;
        private static Configuration config = new Configuration();

        public static void LoadConfiguration()
        {
            try
            {
                var json = File.ReadAllText("config.json");
                config = JsonSerializer.Deserialize<Configuration>(json) ?? new Configuration();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration: {ex.Message}");
            }
        }

        public static void InitializeDatabase()
        {
            if (config?.ConnectionString == null)
            {
                Console.WriteLine("Connection string is not set in the configuration.");
                return;
            }

            using (var connection = new NpgsqlConnection(config.ConnectionString))
            {
                connection.Open();
                try
                {
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT 1 FROM pg_database WHERE datname = 'network_traffic_db'";
                    var dbExists = command.ExecuteScalar() != null;

                    if (!dbExists)
                    {
                        Console.WriteLine("Database not found. Creating...");
                        command.CommandText = "CREATE DATABASE network_traffic_db";
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Database initialization error: {ex.Message}");
                }
            }

            var databaseConnectionString = $"{config.ConnectionString}Database=network_traffic_db";

            using (var connection = new NpgsqlConnection(databaseConnectionString))
            {
                connection.Open();
                try
                {
                    var command = connection.CreateCommand();
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS TrafficData (
                            Id SERIAL PRIMARY KEY,
                            Timestamp TIMESTAMP NOT NULL,
                            SourceIP VARCHAR(45) NOT NULL,
                            DestinationIP VARCHAR(45) NOT NULL,
                            Protocol VARCHAR(10) NOT NULL,
                            PacketSize INT NOT NULL,
                            AnomalyDetected BOOLEAN DEFAULT FALSE
                        )";
                    command.ExecuteNonQuery();

                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Predictions (
                            Id SERIAL PRIMARY KEY,
                            PredictionTimestamp TIMESTAMP NOT NULL,
                            PredictedTrafficVolume INT NOT NULL,
                            ConfidenceLevel FLOAT NOT NULL
                        )";
                    command.ExecuteNonQuery();

                    Console.WriteLine("Successfully created database!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Table creation error: {ex.Message}");
                }
            }
        }

        public static void StartCapture()
        {
            var devices = CaptureDeviceList.Instance;

            device = null;
            foreach (var dev in devices)
            {
                if (dev.Description.Contains(config.CaptureDevice))
                {
                    device = dev;
                    break;
                }
            }

            if (device == null)
            {
                Console.WriteLine("Device not found.");
                return;
            }

            device.OnPacketArrival += new PacketArrivalEventHandler(OnPacketArrival);
            device.Open();
            device.StartCapture();

            Console.WriteLine($"Capturing packets on: {config.CaptureDevice}");
        }

        private static void OnPacketArrival(object sender, PacketCapture e)
        {
            var rawPacket = e.GetPacket();
            var packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
            var ipPacket = packet.Extract<IPPacket>();

            if (ipPacket != null)
            {
                Console.WriteLine($"Packet captured: {ipPacket.SourceAddress} -> {ipPacket.DestinationAddress}");
                DetectAnomaly(ipPacket);
                SavePacketToDatabase(ipPacket);
            }
        }

        private static void DetectAnomaly(IPPacket packet)
        {
            if (packet.Bytes.Length > 1500)
            {
                Console.WriteLine($"Anomaly detected: Packet size {packet.Bytes.Length} exceeds threshold.");
            }
        }

        private static async void SavePacketToDatabase(IPPacket packet)
        {
            packetInsertBatch.Add($"('{DateTime.Now}', '{packet.SourceAddress}', '{packet.DestinationAddress}', '{packet.Protocol}', {packet.Bytes.Length})");

            if (packetInsertBatch.Count >= BatchSize)
            {
                var databaseConnectionString = $"{config.ConnectionString}Database=network_traffic_db";
                using (var connection = new NpgsqlConnection(databaseConnectionString))
                {
                    await connection.OpenAsync();

                    var command = connection.CreateCommand();
                    var insertCommandText = "INSERT INTO TrafficData (Timestamp, SourceIP, DestinationIP, Protocol, PacketSize) VALUES ";
                    insertCommandText += string.Join(", ", packetInsertBatch);
                    command.CommandText = insertCommandText;

                    try
                    {
                        await command.ExecuteNonQueryAsync();
                        packetInsertBatch.Clear();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error saving packet data: {ex.Message}");
                    }
                }
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            PacketCaptureService.LoadConfiguration();
            PacketCaptureService.InitializeDatabase();
            PacketCaptureService.StartCapture();
        }
    }
}
