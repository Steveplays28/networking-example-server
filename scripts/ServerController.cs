using Godot;

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
