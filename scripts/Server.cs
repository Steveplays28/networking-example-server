using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Godot;

public static class Server
{
	#region Varbiables
	public static string mainIp = "127.0.0.1";
	public static string mainPort = "24465";

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
		{ 0, Connect }
	};
	#endregion

	// public override void _Ready()
	// {
	// 	CreateUdpClient(mainIp, mainPort);

	// 	using (Packet packet = new Packet(0, 0, 0))
	// 	{
	// 		packet.WriteData("hi there :)");

	// 		SendPacket(packet);
	// 	}
	// }

	public static void CreateUdpClient(string ip, string port)
	{
		udpState.serverEndPoint = new IPEndPoint(IPAddress.Parse(ip), port.ToInt());
		udpState.udpClient = new UdpClient();
		udpState.packetCount = 0;

		udpState.connectedClients = new Dictionary<int, UdpClient>();
	}

	#region Sending packets
	public static async Task<byte[]> SendPacketToAllAsync(Packet packet)
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

		return packetData;
	}
	/// <summary>
	/// Sends a packet to a client.
	/// </summary>
	/// <param name="packet">The packet to send.</param>
	/// <param name="clientId">The client that the packet should be sent to.</param>
	/// <returns></returns>
	public static async Task<byte[]> SendPacketToAsync(int clientId, Packet packet)
	{
		// Write packet header
		packet.WritePacketHeader();

		// Get data from the packet
		byte[] packetData = packet.ReturnData();

		// Send the packet to the specified client
		udpState.connectedClients.TryGetValue(clientId, out UdpClient udpClient);
		await udpState.udpClient.SendAsync(packetData, packetData.Length, (IPEndPoint)udpClient.Client.LocalEndPoint);

		return packetData;
	}
	#endregion

	#region Receiving packets
	public static async Task<byte[]> ReceivePacketAsync()
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

		return packetData;
	}
	#endregion

	#region Packet callback functions
	public static async Task Connect(int clientId, Packet packet)
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
