using Microsoft.AspNetCore.SignalR;
using Oblak.Interfaces;

namespace Oblak.SignalR;

public class MessageHub : Hub
{
    public void Start()
    {
        //Clients.Caller
    }

    public async Task Notify()
    {
        //Clients.Caller
    }

    public async Task Status(string user, int progress, string message)
    {
        //await Clients.User(user).SendAsync("status", "asdasdsa");
    }

    public void Stop()
    {

    }
}