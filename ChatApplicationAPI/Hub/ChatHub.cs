using ChatApplicationAPI.Models;
using Microsoft.AspNetCore.SignalR;

namespace ChatApplicationAPI.Hub
{
	public class ChatHub : Microsoft.AspNetCore.SignalR.Hub
	{
		//Join the Room

		private readonly IDictionary<string, UserRoomConnection> _connection;

		public ChatHub(IDictionary<string, UserRoomConnection> connection)
		{
			_connection = connection;
		}

		public async Task JoinRoom(UserRoomConnection userConnection)
		{
			//Groups comes from Hub,access the context for connection id ,group name(room)
			await Groups.AddToGroupAsync(Context.ConnectionId,userConnection.Room!);

			_connection[Context.ConnectionId] = userConnection;
			//once the user is added, we can notify the client, sendasync(provide the method name) and we can provide who is sending this message and also we can specify what message we want to send
			await Clients.Group(userConnection.Room!).SendAsync("ReceiveMessage","BotNit", $"{userConnection.User} has joined the group",DateTime.Now);

			await SendConnectedUser(userConnection.Room!);

		}

		//Sending the message

		public async Task SendMessage(string message)
		{
			//check the dictionary
			//if the connectionID is there, then get the value in userRoomConnection
			if (_connection.TryGetValue(Context.ConnectionId , out UserRoomConnection userRoomConnection))
			{
				//Add it in the client 
				//Clients.Group(which room you want to add the connection)
				//SendAsync(method name, user who has sent the message, send the message , at what time the message was sent)
				await Clients.Group(userRoomConnection.Room!).SendAsync("ReceiveMessage" , userRoomConnection.User , message,DateTime.Now);
			}
		}

		//Some one is leaving the group 

		//We are overriding a method in Hub class

		public override Task OnDisconnectedAsync(Exception? exception)
		{
			//if the connection id is not present
			if(!_connection.TryGetValue(Context.ConnectionId, out UserRoomConnection userRoomConnection))
			{
				return base.OnDisconnectedAsync(exception);
			}
			_connection.Remove(Context.ConnectionId);
			//Group(which room)
			//SendAsync(methodname,who is sending, the message to the user)
			Clients.Group(userRoomConnection.Room!).SendAsync("ReceiveMessage","BotNit" , $"{userRoomConnection.User} has left the group");

			//We need to call this method SendConnectedUser to get the connected users

			SendConnectedUser(userRoomConnection.Room!);

			return base.OnDisconnectedAsync(exception);
		}





		//How many users are connected in the group
		//We will send the room name for which we want all the list of users 

		public Task SendConnectedUser(string room) 
		{
			var users = _connection.Values.Where(u=> u.Room == room).Select(s=>s.User);
			//This will return to the client and client(angular) will look for a method called "ConnectedUser"
			//this will be invoked in the angular and it will receive all the users
			return Clients.Group(room).SendAsync("ConnectedUser", users);
		}
	}
}
