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

		// Connected clients
		public Dictionary<IPEndPoint, int> connectedClientsIpToId;
		public Dictionary<int, IPEndPoint> connectedClientsIdToIp;

		// Saved clients
		public Dictionary<IPEndPoint, int> savedClientsIpToId;
		public Dictionary<int, IPEndPoint> savedClientsIdToIp;
	}
	public static UdpState udpState = new UdpState();

	// Packet callback functions
	public static Dictionary<int, Action<Packet, IPEndPoint>> packetFunctions = new Dictionary<int, Action<Packet, IPEndPoint>>()
	{
		{ 0, OnConnect }
	};
	#endregion

	public static void StartServer()
	{
		udpState.serverEndPoint = new IPEndPoint(IPAddress.Parse(ip), port.ToInt());
		udpState.udpClient = new UdpClient(udpState.serverEndPoint);
		udpState.packetCount = 0;

		udpState.connectedClientsIpToId = new Dictionary<IPEndPoint, int>();
		udpState.connectedClientsIdToIp = new Dictionary<int, IPEndPoint>();
		udpState.savedClientsIpToId = new Dictionary<IPEndPoint, int>();
		udpState.savedClientsIdToIp = new Dictionary<int, IPEndPoint>();

		// Create and start a UDP receive thread for Server.ReceivePacket(), so it doesn't block Godot's main thread
		System.Threading.Thread udpReceiveThread = new System.Threading.Thread(new ThreadStart(ReceivePacket))
		{
			Name = "UDP receive thread",
			IsBackground = true
		};
		udpReceiveThread.Start();
		// TODO: Server start try catch block

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
		foreach (IPEndPoint connectedClient in udpState.connectedClientsIdToIp.Values)
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
	public static void SendPacketTo(Packet packet, int recipientId)
	{
		// Write packet header
		packet.WritePacketHeader();

		// Get data from the packet
		byte[] packetData = packet.ReturnData();

		// Send the packet to the specified client
		if (udpState.connectedClientsIdToIp.TryGetValue(recipientId, out IPEndPoint connectedClient))
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
			// Extract data from the received packet
			IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
			byte[] packetData = udpState.udpClient.Receive(ref remoteEndPoint);

			// Construct new Packet object from the received packet
			using (Packet constructedPacket = new Packet(packetData))
			{
				packetFunctions[constructedPacket.connectedFunction].Invoke(constructedPacket, remoteEndPoint);
			}

			System.Threading.Thread.Sleep(17);
		}
	}
	#endregion

	#region Packet callback functions
	public static void OnConnect(Packet packet, IPEndPoint ipEndPoint)
	{
		// Accept the client's connection request
		int createdClientId = udpState.connectedClientsIdToIp.Count;
		udpState.connectedClientsIdToIp.Add(createdClientId, ipEndPoint);
		udpState.connectedClientsIpToId.Add(ipEndPoint, createdClientId);
		// TODO: Check if client isn't already connected

		// Send a new packet back to the newly connected client
		using (Packet newPacket = new Packet(0, 0))
		{
			// Write message of the day to the packet
			newPacket.WriteData("Hello, this is the message of the day! :)");

			SendPacketTo(newPacket, udpState.connectedClientsIpToId[ipEndPoint]);
		}

		ServerController.instance.EmitSignal("OnConnected");
		GD.Print($"{printHeader} New client connected from {ipEndPoint}.");
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
