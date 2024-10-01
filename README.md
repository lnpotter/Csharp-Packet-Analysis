
# C#-Packet-Analysis

## Overview
C#-Packet-Analysis is a C# application designed to capture and analyze network packets, storing them in a PostgreSQL database. It detects anomalies based on packet size and allows for capturing data from a specified network interface.

## Features
- Captures network traffic using `SharpPcap` and `PacketDotNet`.
- Stores packet data in PostgreSQL database.
- Detects anomalies when packet sizes exceed 1500 bytes.
- Batch insertion of captured packet data for efficiency.
- Creates necessary database and tables if they do not exist.

## Prerequisites
- .NET SDK
- PostgreSQL
- A network interface card compatible with SharpPcap.
- Libraries: 
  - `SharpPcap`
  - `PacketDotNet`
  - `Npgsql`

## Setup

1. Clone the repository:
   ```bash
   git clone https://github.com/lnpotter/C#-Packet-Analysis.git
   cd C#-Packet-Analysis
   ```

2. Install the required packages:
   ```bash
   dotnet add package SharpPcap
   dotnet add package PacketDotNet
   dotnet add package Npgsql
   ```

3. Update the `config.json` file with your PostgreSQL connection string and capture device:
   ```json
   {
     "ConnectionString": "Host=localhost;Username=youruser;Password=yourpassword;",
     "CaptureDevice": "your_capture_device_name"
   }
   ```

4. Build the project:
   ```bash
   dotnet build
   ```

5. Run the application:
   ```bash
   dotnet run
   ```

## How It Works

- The application reads from a `config.json` file to retrieve the PostgreSQL connection string and the network device to monitor.
- It attempts to connect to a PostgreSQL database and checks if the `network_traffic_db` database exists. If not, it creates the database and the required tables (`TrafficData` and `Predictions`).
- The `SharpPcap` library is used to capture packets from the specified network interface.
- Captured packets are processed, and anomalies are detected when packets exceed a specified size.
- The packet data is batched and inserted into the PostgreSQL database.

## How to Contribute

1. Fork the repository.
2. Create a new branch with your feature or bug fix:
   ```bash
   git checkout -b feature-or-bugfix-branch
   ```
3. Commit your changes:
   ```bash
   git commit -m "Describe your changes"
   ```
4. Push to the branch:
   ```bash
   git push origin feature-or-bugfix-branch
   ```
5. Open a pull request describing the changes.

## License
This project is licensed under the MIT License. See the `LICENSE` file for more details.

## Contact
For any questions or issues, please open an issue in the repository.
