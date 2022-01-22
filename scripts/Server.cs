using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Godot;

public static class Server
{
	#region Varbiables
	public static string ip = "127.0.0.1";
	public static string port = "24465";

	public struct UdpState
	{
		public IPEndPoint serverEndPoint;
		public UdpClient udpClient;
		// Not implemented
		// TODO: implement packetCount
		public int packetCount;

		// Connected clients
		public Dictionary<int, UdpClient> connectedClients;
		// Saved clients
		public Dictionary<int, UdpClient> savedClients;
	}
	public static UdpState udpState = new UdpState();

	// Packet callback functions
	public static Dictionary<int, Action<int, Packet>> packetFunctions = new Dictionary<int, Action<int, Packet>>()
	{
		{ 0, OnConnect }
	};
	#endregion

	public static void StartServer()
	{
		udpState.serverEndPoint = new IPEndPoint(IPAddress.Parse(ip), port.ToInt());
		udpState.udpClient = new UdpClient();
		udpState.packetCount = 0;

		udpState.connectedClients = new Dictionary<int, UdpClient>();
	}

	#region Sending packets
	/// <summary>
	/// Sends a packet to all clients asynchronously.
	/// </summary>
	/// <param name="packet">The packet to send.</param>
	public static async Task SendPacketToAllAsync(Packet packet)
	{
		// Write packet header
		packet.WritePacketHeader();

		// Get data from the packet
		byte[] packetData = packet.ReturnData();

		// Send the packet to all connected clients
		foreach (UdpClient connectedClient in udpState.connectedClients.Values)
		{
			await udpState.udpClient.SendAsync(packetData, packetData.Length, (IPEndPoint)connectedClient.Client.LocalEndPoint);
		}
	}
	/// <summary>
	/// Sends a packet to a client asynchronously.
	/// </summary>
	/// <param name="packet">The packet to send.</param>
	/// <param name="clientId">The client that the packet should be sent to.</param>
	/// <returns></returns>
	public static async Task SendPacketToAsync(int clientId, Packet packet)
	{
		// Write packet header
		packet.WritePacketHeader();

		// Get data from the packet
		byte[] packetData = packet.ReturnData();

		// Send the packet to the specified client
		udpState.connectedClients.TryGetValue(clientId, out UdpClient udpClient);
		await udpState.udpClient.SendAsync(packetData, packetData.Length, (IPEndPoint)udpClient.Client.LocalEndPoint);
	}
	#endregion

	#region Receiving packets
	public static async Task ReceivePacketAsync()
	{
		UdpReceiveResult receivedPacket = await udpState.udpClient.ReceiveAsync();

		// Extract data from the received packet
		byte[] packetData = receivedPacket.Buffer;
		IPEndPoint remoteEndPoint = receivedPacket.RemoteEndPoint;

		// Construct new Packet object from the received packet
		using (Packet constructedPacket = new Packet(packetData))
		{
			packetFunctions[constructedPacket.connectedFunction].Invoke(constructedPacket.clientId, constructedPacket);
		}
	}
	#endregion

	#region Packet callback functions
	public static async void OnConnect(int clientId, Packet packet)
	{
		// Accept the client's connection request
		clientId = udpState.savedClients.Count;
		UdpClient connectedClient = new UdpClient(udpState.serverEndPoint);
		udpState.connectedClients.Add(clientId, connectedClient);

		// Send a new packet back to the newly connected client
		using (Packet newPacket = new Packet(0, 0, clientId))
		{
			// For future implementation, write data to the connect packet here

			await SendPacketToAsync(clientId, newPacket);
		}
	}
	#endregion
}

public class ServerController : Node
{
	public override void _Ready()
	{
		Server.ip = "127.0.0.1";
		Server.port = "24465";

		Server.StartServer();
	}

	public override async void _Process(float delta)
	{
		await Server.ReceivePacketAsync();
	}
}
