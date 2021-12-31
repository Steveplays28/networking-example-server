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
		SendData("hello client, i'm the server :D");
		SendData(7);
	}

	public static void CreateUdpClient(string ip, string port)
	{
		udpState.endPoint = new IPEndPoint(IPAddress.Parse(ip), port.ToInt());
		udpState.udpClient = new UdpClient();
		udpState.packetCount = 0;
	}

	// Byte array integer prefixes:
	// 0 = bool
	// 1 = integer
	// 2 = float
	// 3 = string
	public static void SendData(bool data)
	{
		// Integer prefix for byte array
		int prefix = 0;

		// Create a byte list with the prefix
		List<byte> byteList = new List<byte>(BitConverter.GetBytes(prefix));
		byteList.AddRange(BitConverter.GetBytes(data));

		byte[] byteArray = byteList.ToArray();

		// Send the message (the destination is defined by the server name and port)
		udpState.udpClient.BeginSend(byteArray, byteArray.Length, udpState.endPoint, new AsyncCallback(SendCallback), udpState.udpClient);
	}
	public static void SendData(int data)
	{
		// Integer prefix for byte array
		int prefix = 1;

		// Create a byte list with the prefix
		List<byte> byteList = new List<byte>(BitConverter.GetBytes(prefix));
		byteList.AddRange(BitConverter.GetBytes(data));

		byte[] byteArray = byteList.ToArray();

		// Send the message (the destination is defined by the server name and port)
		udpState.udpClient.BeginSend(byteArray, byteArray.Length, udpState.endPoint, new AsyncCallback(SendCallback), udpState.udpClient);
	}
	public static void SendData(float data)
	{
		// Integer prefix for byte array
		int prefix = 2;

		// Create a byte list with the prefix
		List<byte> byteList = new List<byte>(BitConverter.GetBytes(prefix));
		byteList.AddRange(BitConverter.GetBytes(data));

		byte[] byteArray = byteList.ToArray();

		// Send the message (the destination is defined by the server name and port)
		udpState.udpClient.BeginSend(byteArray, byteArray.Length, udpState.endPoint, new AsyncCallback(SendCallback), udpState.udpClient);
	}
	public static void SendData(string data)
	{
		// Integer prefix for byte array
		int prefix = 3;

		// Create a byte list with the prefix
		List<byte> byteList = new List<byte>(BitConverter.GetBytes(prefix));
		byteList.AddRange(Encoding.UTF8.GetBytes(data));

		byte[] byteArray = byteList.ToArray();

		// Debug stuff for my sanity :)
		// GD.Print($"length of prefix: {BitConverter.GetBytes(prefix).Length}");
		// GD.Print($"prefix: {BitConverter.ToInt32(byteArray, 0)}");
		// GD.Print($"data: {Encoding.UTF8.GetString(byteArray, 4, byteArray.Length - 4)}");
		// GD.Print($"byte array: {ToReadableByteArray(byteArray)}");

		// Send the message (the destination is defined by the server name and port)
		udpState.udpClient.BeginSend(byteArray, byteArray.Length, udpState.endPoint, new AsyncCallback(SendCallback), udpState.udpClient);
	}
	private static void SendCallback(IAsyncResult asyncResult)
	{
		UdpClient udpClient = (UdpClient)asyncResult.AsyncState;

		GD.Print($"number of bytes sent: {udpClient.EndSend(asyncResult)}");
	}

	// Debug function for my sanity :)
	public static string ToReadableByteArray(byte[] bytes)
	{
		return string.Join(", ", bytes);
	}
}
