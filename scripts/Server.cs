using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
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
		public Dictionary<int, UdpClient> connectedClients;
		// Saved clients
		public Dictionary<int, UdpClient> savedClients;
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
		udpState.connectedClients = new Dictionary<int, UdpClient>();
		udpState.savedClients = new Dictionary<int, UdpClient>();

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
		foreach (UdpClient connectedClient in udpState.connectedClients.Values)
		{
			udpState.udpClient.Send(packetData, packetData.Length, (IPEndPoint)connectedClient.Client.LocalEndPoint);
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
		udpState.connectedClients.TryGetValue(packet.recipientId, out UdpClient udpClient);
		udpState.udpClient.Send(packetData, packetData.Length, (IPEndPoint)udpClient.Client.LocalEndPoint);
	}
	#endregion

	#region Receiving packets
	public static void ReceivePacket()
	{
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
	#endregion

	#region Packet callback functions
	public static void OnConnect(Packet packet)
	{
		// Accept the client's connection request
		int createdClientId = udpState.savedClients.Count;
		// TODO: Replace UdpClient with IPEndPoint of clients, and pass the IP to the function (maybe with a dynamic argument?)
		UdpClient connectedClient = new UdpClient();
		udpState.connectedClients.Add(createdClientId, connectedClient);

		GD.Print($"{printHeader} New client connected from {connectedClient.Client.LocalEndPoint}");

		// Send a new packet back to the newly connected client
		using (Packet newPacket = new Packet(0, 0, udpState.serverId, createdClientId))
		{
			// Write the recipient's IP address back to them
			newPacket.WriteData(connectedClient.Client.LocalEndPoint.ToString());
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
