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
	}
	public static UdpState udpState = new UdpState();

	public override void _Ready()
	{
		CreateUdpClient(mainIp, mainPort);

		using (Packet packet = new Packet(0, 0, 0))
		{
			packet.WriteData("hi");

			packet.WritePacketHeader();
			SendPacket(packet);
		}
	}

	public static void CreateUdpClient(string ip, string port)
	{
		udpState.endPoint = new IPEndPoint(IPAddress.Parse(ip), port.ToInt());
		udpState.udpClient = new UdpClient();
		udpState.packetCount = 0;
	}

	public static void SendPacket(Packet packet)
	{
		// Write packet header
		packet.WritePacketHeader();

		// Get data from packet
		byte[] packetData = packet.ReturnData();

		// Send the message (the destination is defined by the server name and port)
		udpState.udpClient.BeginSend(packetData, packetData.Length, udpState.endPoint, new AsyncCallback(SendPacketCallback), udpState.udpClient);
	}
	private static void SendPacketCallback(IAsyncResult asyncResult)
	{
		UdpClient udpClient = (UdpClient)asyncResult.AsyncState;

		GD.Print($"number of bytes sent: {udpClient.EndSend(asyncResult)}");
	}
}
