using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Godot;

public class Server : Node
{
	public static string mainIp = "127.0.0.1";
	public static string mainPort = "24465";

	public struct UdpState
	{
		public IPEndPoint endPoint;
		public UdpClient udpClient;
		public int packetCount;
		// TODO: implement packet ids and checks

		// Stored and connected clients (clientId, udpClient)
		public Dictionary<int, UdpClient> savedClients;
		public Dictionary<int, UdpClient> connectedClients;
		// TODO: implement client connections
		// TODO: implement afk kick
	}
	public static UdpState udpState = new UdpState();

	public static Dictionary<int, Action<int, Packet>> packetFunctions = new Dictionary<int, Action<int, Packet>>()
	{
		{ 0, OnConnected }
	};

	public override void _Ready()
	{
		CreateUdpClient(mainIp, mainPort);

		using (Packet packet = new Packet(0, 0, -1))
		{
			packet.WriteData("hi there :)");

			SendPacket(packet);
		}
	}

	public static void CreateUdpClient(string ip, string port)
	{
		udpState.endPoint = new IPEndPoint(IPAddress.Parse(ip), port.ToInt());
		udpState.udpClient = new UdpClient();
		udpState.packetCount = 0;

		udpState.connectedClients = new Dictionary<int, UdpClient>();
	}

	public static bool IsClientSaved(int clientId)
	{
		return udpState.savedClients.ContainsKey(clientId);
	}
	public static bool IsClientSaved(IPEndPoint clientEndPoint)
	{
		bool isClientSaved = false;

		foreach (UdpClient udpClient in udpState.savedClients.Values)
		{
			if (udpClient.Client.LocalEndPoint == clientEndPoint)
			{
				isClientSaved = true;
			}
		}

		return isClientSaved;
	}

	public static bool IsClientConnected(int clientId)
	{
		return udpState.connectedClients.ContainsKey(clientId);
	}

	#region Sending packets
	public static void SendPacket(Packet packet)
	{
		// Write packet header
		packet.WritePacketHeader();

		// Get data from packet
		byte[] packetData = packet.ReturnData();

		// Send the message (the destination is defined by the server name and port)
		udpState.udpClient.BeginSend(packetData, packetData.Length, udpState.endPoint, new AsyncCallback(SendPacketCallback), udpState.udpClient);

		// TODO: error handling
	}
	private static void SendPacketCallback(IAsyncResult asyncResult)
	{
		UdpClient udpClient = (UdpClient)asyncResult.AsyncState;

		GD.Print($"Amount of bytes sent: {udpClient.EndSend(asyncResult)}");
	}

	public static void SendPacketTo(int clientId, Packet packet)
	{
		UdpClient udpClient = udpState.connectedClients[clientId];

		// Write packet header
		packet.WritePacketHeader();

		// Get data from packet
		byte[] packetData = packet.ReturnData();

		udpClient.BeginSend(packetData, packetData.Length, new AsyncCallback(SendPacketToCallback), udpState.udpClient);
	}
	private static void SendPacketToCallback(IAsyncResult asyncResult)
	{
		UdpClient udpClient = (UdpClient)asyncResult.AsyncState;

		GD.Print($"Amount of bytes sent: {udpClient.EndSend(asyncResult)}");
	}
	#endregion

	#region Receiving packets
	private static void ReceiveCallback(IAsyncResult asyncResult)
	{
		// Called when a packet is received
		IPEndPoint senderEndPoint = new IPEndPoint(IPAddress.Any, int.Parse(mainPort));
		byte[] receiveBytes = udpState.udpClient.EndReceive(asyncResult, ref senderEndPoint);
		udpState.packetCount += 1;

		// Continue listening for packets
		udpState.udpClient.BeginReceive(new AsyncCallback(ReceiveCallback), udpState.udpClient);

		// Handle received packet
		using (Packet packet = new Packet(receiveBytes))
		{
			// Debug, lol
			GD.Print(string.Join(",", receiveBytes));

			// Check if client exists in the savedClients dictionary
			if (!IsClientSaved(senderEndPoint))
			{
				// If not, add the client to the savedClients dictionary
				int clientId = udpState.savedClients.Count;
				UdpClient udpClient = new UdpClient(senderEndPoint);

				udpState.savedClients.Add(clientId, udpClient);
			}

			// TODO: check if client exists in connectedClients dictionary

			// Invoke the packet's connected function
			packetFunctions[packet.connectedFunction].Invoke(packet.clientId, packet);

			// TODO: check for dropped packets using packetNumber, and let the server resend a list of packets if needed
			// TODO: use checksum to check for data corruption/data loss in the packet
		}
	}
	#endregion

	#region Packet callbacks
	// Packet callback functions must be static, else they cannot be stored in the packetFunctions dictionary
	private static void OnConnected(int clientId, Packet packet)
	{
		using (Packet newPacket = new Packet(0, 0, clientId))
		{
			newPacket.WriteData("hi there :)");

			SendPacket(newPacket);
		}
	}
	#endregion
}
