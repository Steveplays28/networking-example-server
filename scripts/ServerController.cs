using Godot;

public class ServerController : Node
{
	public override void _Ready()
	{
		Server.ip = "127.0.0.1";
		Server.port = "24476";

		Server.StartServer();
	}

	public override void _Process(float delta)
	{
		Server.ReceivePacket();

		if (Input.IsActionJustReleased("ui_end"))
		{
			GetTree().Quit();
		}
	}

	public override void _ExitTree()
	{
		Server.CloseUdpClient();
	}
}
