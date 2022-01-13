using System;
using System.IO;
using System.Security.Cryptography;

/// <summary>
/// Writeable packet, dispose using Dispose() when the packet is no longer in use.
/// </summary>
public class Packet : IDisposable
{
	public byte packetNumber;
	public byte connectedFunction;
	public int clientId;

	private MemoryStream memoryStream;
	private BinaryWriter binaryWriter;
	private BinaryReader binaryReader;

	public Packet(byte packetNumber, byte connectedFunction, int clientId)
	{
		memoryStream = new MemoryStream();
		binaryWriter = new BinaryWriter(memoryStream);
		binaryReader = new BinaryReader(memoryStream);

		this.packetNumber = packetNumber;
		this.connectedFunction = connectedFunction;
		this.clientId = clientId;
	}
	public Packet(byte[] byteArray)
	{
		memoryStream = new MemoryStream();
		binaryWriter = new BinaryWriter(memoryStream);
		binaryReader = new BinaryReader(memoryStream);

		binaryWriter.Write(byteArray);
		memoryStream.Position = 0;

		packetNumber = ReadByte();
		connectedFunction = ReadByte();
		clientId = ReadInt32();
	}

	#region WriteData
	/// <summary>
	/// Writes a header to the packet (containing the number of the packet, the connected function of the packet, the length of the packet's contents, and a checksum if enabled). <br/>
	/// Make sure to do this after all data has been written to the packet!
	/// </summary>
	public void WritePacketHeader()
	{
		binaryWriter.Write(packetNumber);
		binaryWriter.Write(connectedFunction);
		binaryWriter.Write(clientId);

		// Checksum
		// binaryWriter.Write(CalculateChecksum());
	}

	public void WriteData(bool data)
	{
		// Write data to packet
		binaryWriter.Write(data);
	}
	public void WriteData(int data)
	{
		// Write data to packet
		binaryWriter.Write(data);
	}
	public void WriteData(float data)
	{
		// Write data to packet
		binaryWriter.Write(data);
	}
	public void WriteData(string data)
	{
		// Write length prefix and data to packet
		binaryWriter.Write(data);
	}
	#endregion

	#region ReadData
	/// <returns>
	/// Packet number (byte), connected function (byte), client ID (int)
	/// </returns> 
	public (byte, byte, int) ReadPacketHeader()
	{
		return (binaryReader.ReadByte(), binaryReader.ReadByte(), binaryReader.ReadInt32());

		// Checksum
		// Read checksum, not implemented yet
	}

	public bool ReadBoolean()
	{
		return binaryReader.ReadBoolean();
	}
	public byte ReadByte()
	{
		return binaryReader.ReadByte();
	}
	public int ReadInt32()
	{
		return binaryReader.ReadInt32();
	}
	public float ReadFloat()
	{
		return binaryReader.ReadSingle();
	}
	public string ReadString()
	{
		return binaryReader.ReadString();
	}
	#endregion

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
		binaryReader.Dispose();
	}
}
