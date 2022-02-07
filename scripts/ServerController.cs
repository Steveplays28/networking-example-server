using Godot;

public class ServerController : Node
{
	public static ServerController instance;

	public override void _Ready()
	{
		if (instance == null)
		{
			instance = this;
		}
		else
		{
			GD.PushWarning("ServerController instance is already set, overriding!");
		}

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
