using Microsoft.AspNetCore.SignalR;
using Oblak.Interfaces;

namespace Oblak.SignalR;

public class MessageHub : Hub
{
	public static List<ConnectedUser> _connectedUsers = new List<ConnectedUser>();

	public override Task OnConnectedAsync()
	{
		var username = Context.GetHttpContext().User.Identity.Name;

		var status = _connectedUsers.FirstOrDefault(x => x.Username.ToLower() == username.ToLower());

		if (status == null)
		{
			_connectedUsers.Add(new ConnectedUser
			{
				ConnectionId = Context.ConnectionId,
				Username = username
			});
		}
		else
		{ 
			status.ConnectionId = Context.ConnectionId;
		}	

		return base.OnConnectedAsync();
	}

	public void Start()
    {
        //Clients.Caller
    }

    public async Task Notify()
    {
        //Clients.Caller
    }

    public async Task Status(string user, int progress, string message, string guest)
    {
		//await Clients.Users(user).Status(user, progress, message, guest);	

		_connectedUsers.ForEach(val =>
		{
			if (val.Username == user) Clients.Clients(val.ConnectionId).SendAsync("status", progress, message, guest);
		});

		//await Clients.User(user).SendAsync("status", "asdasdsa");
	}

	public async Task Status1(string user, int progress, string message, string guest)
	{
		_connectedUsers.ForEach(val =>
		{
			//if (val.Username == user)
			//	Clients.Clients(val.ConnectionId).SendAsync("status", progress, message, guest);
		});

		//await Clients.User(user).SendAsync("status", "asdasdsa");
	}

	public void Stop()
    {

    }
}

public class ConnectedUser
{ 
	public string Username { get; set; }

	public string ConnectionId { get; set;}
}

public interface IMessageHub
{
	Task Status(string user, int progress, string message, string guest);
}