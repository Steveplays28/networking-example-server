using Godot;

public class ServerController : Node
{
	public static ServerController instance;

	[Signal]
	public delegate void Connected(int clientId, string messageOfTheDay);
	[Signal]
	public delegate void Disconnected();

	public override void _Ready()
	{
		if (instance != null)
		{
			GD.PushWarning("ServerController instance is already set, overriding!");
		}
		instance = this;

		// Set the endpoint to run the server on
		Server.ip = "127.0.0.1";
		Server.port = "24476";

		// Start server
		Server.StartServer();
	}

	public override void _Process(float delta)
	{
		if (Input.IsActionJustReleased("ui_cancel"))
		{
			GetTree().Quit();
		}
	}

	public override void _ExitTree()
	{
		Server.CloseUdpClient();
	}
}
