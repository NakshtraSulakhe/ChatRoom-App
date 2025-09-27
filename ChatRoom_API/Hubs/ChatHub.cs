using ChatRoom_API.Interface;
<<<<<<< HEAD
using ChatRoom_API.Interfecae;
=======
>>>>>>> a4a5677 (Updated frontend (Angular) and backend (.NET) with new features)
using ChatRoom_API.Service;
using Microsoft.AspNetCore.SignalR;
using ChatRoom_API.Hubs;
using ChatRoom_API.Models;
using System.Collections.Concurrent;


namespace ChatRoom_API.Hubs
{
    public class ChatMessageDto
    {
        public string UserId { get; set; }

        public string Username { get; set; }
        public string Text { get; set; }
        public string Timestamp { get; set; }
    }

    public class ChatHub : Hub
    {
        private readonly IOnlineUserService _onlineUserService;
        private readonly ILogger<ChatHub> _logger;
        private readonly IMessageService _messageService; // Add this


        // ✅ Chat history stored in memory (for demo purposes)
        private static readonly List<ChatMessage> _chatHistory = new();

        public ChatHub(IOnlineUserService onlineUserService, ILogger<ChatHub> logger, IMessageService messageService)
        {
            _onlineUserService = onlineUserService;
            _logger = logger;
            _messageService = messageService;
        }

        public override async Task OnConnectedAsync()
        {
<<<<<<< HEAD
            var httpContext = Context.GetHttpContext();
            var username = httpContext?.Request.Query["username"].ToString();

            if (string.IsNullOrEmpty(username))
            {
                Console.WriteLine("❌ Connection rejected. No username found.");
                return;
            }

            _onlineUserService.AddUser(username, Context.ConnectionId);

            // 🔥 Load chat history
            var history = await _messageService.GetChatHistory();
            await Clients.Caller.SendAsync("LoadChatHistory", history);

            // 👥 Update online users
            var onlineUsers = _onlineUserService.GetOnlineUsers();
            await Clients.All.SendAsync("UpdateUserList", onlineUsers);
            await Clients.Others.SendAsync("UserJoined", username);

            await base.OnConnectedAsync();
=======
            try
            {
                var httpContext = Context.GetHttpContext();
                var username = httpContext?.Request.Query["username"].ToString();

                if (string.IsNullOrEmpty(username))
                {
                    _logger.LogWarning("Connection rejected. No username found for connection {ConnectionId}", Context.ConnectionId);
                    await Clients.Caller.SendAsync("ConnectionError", "Username is required");
                    return;
                }

                _logger.LogInformation("User {Username} connecting with connection {ConnectionId}", username, Context.ConnectionId);
                _onlineUserService.AddUser(username, Context.ConnectionId);

                // 🔥 Load chat history
                try
                {
                    var history = await _messageService.GetChatHistory();
                    await Clients.Caller.SendAsync("LoadChatHistory", history);
                    _logger.LogDebug("Chat history loaded for user {Username}", username);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading chat history for user {Username}", username);
                    await Clients.Caller.SendAsync("LoadChatHistory", new List<ChatMessage>());
                }

                // 👥 Update online users
                try
                {
                    var onlineUsers = _onlineUserService.GetOnlineUsers();
                    await Clients.All.SendAsync("UpdateUserList", onlineUsers);
                    await Clients.Others.SendAsync("UserJoined", username);
                    _logger.LogInformation("User {Username} joined. Online users: {OnlineUsers}", username, string.Join(", ", onlineUsers));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating user list for user {Username}", username);
                }

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnConnectedAsync for connection {ConnectionId}", Context.ConnectionId);
                await Clients.Caller.SendAsync("ConnectionError", "An error occurred during connection");
            }
>>>>>>> a4a5677 (Updated frontend (Angular) and backend (.NET) with new features)
        }


        public override async Task OnDisconnectedAsync(Exception? exception)
        {
<<<<<<< HEAD
            var username = _onlineUserService.GetUsernameByConnectionId(Context.ConnectionId);
            if (!string.IsNullOrEmpty(username))
            {
                _onlineUserService.RemoveUser(Context.ConnectionId);
                Console.WriteLine($"❌ {username} disconnected ({Context.ConnectionId})");

                var onlineUsers = _onlineUserService.GetOnlineUsers();
                Console.WriteLine($"📢 Online Users after disconnect: {string.Join(", ", onlineUsers)}");

                await Clients.All.SendAsync("UpdateUserList", onlineUsers);
                await Clients.Others.SendAsync("UserLeft", username);
            }

            await base.OnDisconnectedAsync(exception);
=======
            try
            {
                var username = _onlineUserService.GetUsernameByConnectionId(Context.ConnectionId);
                if (!string.IsNullOrEmpty(username))
                {
                    _onlineUserService.RemoveUser(Context.ConnectionId);
                    _logger.LogInformation("User {Username} disconnected ({ConnectionId})", username, Context.ConnectionId);

                    try
                    {
                        var onlineUsers = _onlineUserService.GetOnlineUsers();
                        _logger.LogInformation("Online Users after disconnect: {OnlineUsers}", string.Join(", ", onlineUsers));

                        await Clients.All.SendAsync("UpdateUserList", onlineUsers);
                        await Clients.Others.SendAsync("UserLeft", username);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error updating user list after disconnect for user {Username}", username);
                    }
                }
                else
                {
                    _logger.LogWarning("Disconnected user not found in connection mapping for connection {ConnectionId}", Context.ConnectionId);
                }

                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnDisconnectedAsync for connection {ConnectionId}", Context.ConnectionId);
                await base.OnDisconnectedAsync(exception);
            }
>>>>>>> a4a5677 (Updated frontend (Angular) and backend (.NET) with new features)
        }

        // ======= Public Message =======
        public async Task SendMessage(string message)
        {
            var username = Context.GetHttpContext()?.Request.Query["username"].ToString();
            if (string.IsNullOrEmpty(username)) username = "Unknown";

            var userId = Guid.NewGuid().ToString(); // or real auth claim
            var timestamp = DateTime.UtcNow;

            var chatMessageEntity = new ChatMessage
            {
                UserId = userId,
                UserName = username,
                Message = message,
                Timestamp = timestamp
            };

            await _messageService.SaveMessageAsync(chatMessageEntity);

            var chatMessageDto = new ChatMessageDto
            {
                UserId = userId,
                Username = username,
                Text = message,
                Timestamp = timestamp.ToString("HH:mm:ss")
            };

            Console.WriteLine($"💬 Public Message from {username}: {message}");

            await Clients.All.SendAsync("ReceiveMessage", chatMessageDto.Username, chatMessageDto.Text, chatMessageDto.Timestamp);
            await Clients.AllExcept(Context.ConnectionId).SendAsync("ReceiveNotification", $"{username} sent a message");
        }



        // ======= Private Message =======
        public async Task SendPrivateMessage(string recipientUsername, string message)
        {
            var senderName = Context.GetHttpContext()?.Request.Query["username"];
            if (string.IsNullOrEmpty(senderName) || string.IsNullOrEmpty(recipientUsername) || string.IsNullOrEmpty(message))
            {
                await Clients.Caller.SendAsync("MessageError", "Recipient and message cannot be empty.");
                return;
            }

            var recipientConnections = _onlineUserService.GetConnectionsByUsername(recipientUsername);
            if (recipientConnections.Any())
            {
                Console.WriteLine($"📩 Private Message from {senderName} to {recipientUsername}: {message}");

                foreach (var connectionId in recipientConnections)
                {
                    await Clients.Client(connectionId).SendAsync("ReceivePrivateMessage", senderName, message);
                }

                await Clients.Caller.SendAsync("MessageSent", recipientUsername, message);
                await Clients.Clients(recipientConnections).SendAsync("ReceiveNotification", $"{senderName} sent you a private message");
            }
            else
            {
                await Clients.Caller.SendAsync("UserNotAvailable", recipientUsername);
                Console.WriteLine($"❌ {recipientUsername} is not online.");
            }
        }

         //✅ Add user to online list manually
        public async Task AddUserToOnlineList(string username)
        {
            if (!string.IsNullOrEmpty(username))
            {
                _onlineUserService.AddUser(username, Context.ConnectionId);
                await Clients.All.SendAsync("UserStatusChanged", username, "connected");
            }
        }

        public void RemoveUserByUsername(string username)
        {
            _onlineUserService.RemoveUserByUsername(username);
        }

        // ======= Typing Indicator =======
        public async Task SendTypingIndicator()
        {
            var username = Context.GetHttpContext()?.Request.Query["username"];
            if (string.IsNullOrEmpty(username)) return;

            _onlineUserService.AddTypingUser(username);
            await Clients.Others.SendAsync("UserTyping", username);
        }

        public async Task StopTypingIndicator()
        {
            var username = Context.GetHttpContext()?.Request.Query["username"];
            if (string.IsNullOrEmpty(username)) return;

            _onlineUserService.RemoveTypingUser(username);
            await Clients.Others.SendAsync("UserStoppedTyping", username);
        }

        public List<string> GetAllOnlineUsers()
        {
            return _onlineUserService.GetOnlineUsers();
        }

        public async Task RequestHistory()
        {
            var messages = await _messageService.GetChatHistory(); // or however you get it
            await Clients.Caller.SendAsync("ReceiveHistory", messages);
        }

    }
}

