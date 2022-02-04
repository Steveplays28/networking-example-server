using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Godot;

public static class Server
{
	#region Varbiables
	public static string ip = "127.0.0.1";
	public static string port = "24464";

	// Header for GD.Print() messages, default = "[Server]:"
	public static string printHeader = "[Server]:";

	public struct UdpState
	{
		public IPEndPoint serverEndPoint;
		public UdpClient udpClient;
		// TODO: Implement packetCount
		public int packetCount;

		// Server ID, default = -1
		public int serverId;
		// The ID used to send packets to newly connected clients or all clients, default = -1
		public int allClientsId;
		// Connected clients
		public Dictionary<int, IPEndPoint> connectedClients;
		// Saved clients
		public Dictionary<int, IPEndPoint> savedClients;
	}
	public static UdpState udpState = new UdpState();

	// Packet callback functions
	public static Dictionary<int, Action<Packet>> packetFunctions = new Dictionary<int, Action<Packet>>()
	{
		{ 0, OnConnect }
	};
	#endregion

	public static void StartServer()
	{
		udpState.serverEndPoint = new IPEndPoint(IPAddress.Parse(ip), port.ToInt());
		udpState.udpClient = new UdpClient(udpState.serverEndPoint);
		udpState.packetCount = 0;

		udpState.serverId = -1;
		udpState.allClientsId = -1;
		udpState.connectedClients = new Dictionary<int, IPEndPoint>();
		udpState.savedClients = new Dictionary<int, IPEndPoint>();

		// Create and start a UDP receive thread for Server.ReceivePacket(), so it doesn't block Godot's main thread
		System.Threading.Thread udpReceiveThread = new System.Threading.Thread(new ThreadStart(ReceivePacket));
		udpReceiveThread.Name = "UDP receive thread";
		udpReceiveThread.Start();

		GD.Print($"{printHeader} Server started on {udpState.serverEndPoint}.");
	}

	#region Sending packets
	/// <summary>
	/// Sends a packet to all clients.
	/// <br/>
	/// Important: the packet's recipientId should always be zero for the packet to be interpreted correctly on the clients.
	/// </summary>
	/// <param name="packet">The packet to send.</param>
	public static void SendPacketToAll(Packet packet)
	{
		// Write packet header
		packet.WritePacketHeader();

		// Get data from the packet
		byte[] packetData = packet.ReturnData();

		// Send the packet to all connected clients
		foreach (IPEndPoint connectedClient in udpState.connectedClients.Values)
		{
			udpState.udpClient.Send(packetData, packetData.Length, connectedClient);
		}
	}
	/// <summary>
	/// Sends a packet to a client.
	/// </summary>
	/// <param name="packet">The packet to send.</param>
	/// <param name="recipientId">The client that the packet should be sent to.</param>
	/// <returns></returns>
	public static void SendPacketTo(Packet packet)
	{
		// Write packet header
		packet.WritePacketHeader();

		// Get data from the packet
		byte[] packetData = packet.ReturnData();

		// Send the packet to the specified client
		if (udpState.connectedClients.TryGetValue(packet.recipientId, out IPEndPoint connectedClient))
		{
			udpState.udpClient.Send(packetData, packetData.Length, connectedClient);
		}
	}
	#endregion

	#region Receiving packets
	public static void ReceivePacket()
	{
		while (true)
		{
			System.Threading.Thread.Sleep(17);

			// Extract data from the received packet
			IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
			byte[] packetData = udpState.udpClient.Receive(ref remoteEndPoint);

			// Construct new Packet object from the received packet
			using (Packet constructedPacket = new Packet(packetData))
			{
				if (constructedPacket.recipientId == udpState.serverId)
				{
					packetFunctions[constructedPacket.connectedFunction].Invoke(constructedPacket);
				}
				else
				{
					GD.PrintErr($"{printHeader} Received a recipientId of {constructedPacket.recipientId}, which isn't equal to the serverId of {udpState.serverId}!");
				}
			}
		}
	}
	#endregion

	#region Packet callback functions
	public static void OnConnect(Packet packet)
	{
		// Accept the client's connection request
		string connectedClientIP = packet.ReadString();
		IPEndPoint connectedClientIPEndPoint = new IPEndPoint(connectedClientIP.Split(":")[0].ToInt(), connectedClientIP.Split(":")[1].ToInt());
		int createdClientId = udpState.savedClients.Count;

		// TODO: Replace UdpClient with IPEndPoint of clients, and pass the IP to the function (maybe with a dynamic argument?)
		udpState.connectedClients.Add(createdClientId, connectedClientIPEndPoint);
		GD.Print($"{printHeader} New client connected from {connectedClientIPEndPoint}");

		// Send a new packet back to the newly connected client
		using (Packet newPacket = new Packet(0, 0, udpState.serverId, createdClientId))
		{
			// Write the recipient's IP address back to them
			newPacket.WriteData(connectedClientIPEndPoint.ToString());
			// TODO: Check if sending the IP is safe... because it's probably very unsafe

			// Write welcome message to the packet
			newPacket.WriteData("Hello, this is the message of the day! :)");

			// SendPacketTo(newPacket);
		}
	}
	#endregion

	public static void CloseUdpClient()
	{
		try
		{
			udpState.udpClient.Close();
			GD.Print($"{printHeader} Successfully closed the UdpClient!");
		}
		catch (SocketException e)
		{
			GD.PrintErr($"{printHeader} Failed closing the UdpClient: {e}");
		}
	}
}
