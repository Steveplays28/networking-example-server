using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using Godot;

/// <summary>
/// Writeable packet, dispose using Dispose().
/// </summary>
public class Packet : IDisposable
{
	public int packetNumber;
	public int connectedFunction;

	private MemoryStream memoryStream;
	private BinaryWriter binaryWriter;

	public Packet()
	{
		memoryStream = new MemoryStream();
		binaryWriter = new BinaryWriter(memoryStream);
	}

	/// <summary>
	/// Writes a header to the packet (containing the number of the packet, the connected function of the packet, the length of the packet's contents, and a checksum if enabled). <br/>
	/// Make sure to do this after all data has been written to the packet!
	/// </summary>
	public void WritePacketHeader()
	{
		binaryWriter.Write(packetNumber);
		binaryWriter.Write(connectedFunction);
		binaryWriter.Write((int)memoryStream.Length);
		// Checksum
		// binaryWriter.Write(CalculateChecksum());
	}

	// Byte array integer prefixes:
	// 0 = bool
	// 1 = integer
	// 2 = float
	// 3 = string
	public void WriteData(bool data)
	{
		// Write type of data (integer) to packet
		binaryWriter.Write(0);

		// Write data to packet
		binaryWriter.Write(data);
	}
	public void WriteData(int data)
	{
		// Write type of data (integer) to packet
		binaryWriter.Write(1);

		// Write data to packet
		binaryWriter.Write(data);
	}
	public void WriteData(float data)
	{
		// Write type of data (integer) to packet
		binaryWriter.Write(2);

		// Write data to packet
		binaryWriter.Write(data);
	}
	public void WriteData(string data)
	{
		// Write type of data (integer) to packet
		binaryWriter.Write(3);

		// Write length prefix and data to packet
		binaryWriter.Write(data);
	}

	/// <summmary>
	/// Calculates a SHA256 checksum from the binary writer's base stream.
	/// </summmary>
	private string CalculateChecksum()
	{
		using (var sha256 = SHA256.Create())
		{
			byte[] checksum = sha256.ComputeHash(memoryStream);
			return BitConverter.ToString(checksum).Replace("-", "").ToLowerInvariant();
		}
	}

	/// <summmary>
	/// Returns the packet's data as a byte array. <br/>
	/// Do not use if the packet is still being written to.
	/// </summmary>
	public byte[] ReturnData()
	{
		// Write all pending data to memory stream
		binaryWriter.Flush();

		// Return byte array
		return memoryStream.ToArray();
	}

	public void Dispose()
	{
		memoryStream.Dispose();
		binaryWriter.Dispose();
	}
}