using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatRoom_API.Interface;
using ChatRoom_API.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace ChatRoom_API.Hubs
{
    public class ChatMessageDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
    }

    public class ChatHub : Hub
    {
        private readonly IOnlineUserService _onlineUserService;
        private readonly ILogger<ChatHub> _logger;
        private readonly IMessageService _message_service;

        // Chat history stored in memory (for demo purposes)
        private static readonly List<ChatMessage> _chatHistory = new();

        public ChatHub(IOnlineUserService onlineUserService, ILogger<ChatHub> logger, IMessageService messageService)
        {
            _onlineUserService = onlineUserService;
            _logger = logger;
            _message_service = messageService;
        }

        // Helper to safely read "username" from the HTTP query (avoids null-conditional on StringValues)
        private static string? GetUsernameFromHttp(HubCallerContext context)
        {
            var httpContext = context.GetHttpContext();
            if (httpContext?.Request.Query.TryGetValue("username", out var val) == true)
                return val.ToString();
            return null;
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                var username = GetUsernameFromHttp(Context);
        
                if (string.IsNullOrEmpty(username))
                {
                    _logger.LogWarning("Connection rejected. No username found for connection {ConnectionId}", Context.ConnectionId);
                    await Clients.Caller.SendAsync("ConnectionError", "Username is required");
                    return;
                }

                _logger.LogInformation("User {Username} connecting with connection {ConnectionId}", username, Context.ConnectionId);
                _onlineUserService.AddUser(username, Context.ConnectionId);

                // Load chat history
                try
                {
                    var history = await _message_service.GetChatHistory();
                    await Clients.Caller.SendAsync("LoadChatHistory", history);
                    _logger.LogDebug("Chat history loaded for user {Username}", username);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading chat history for user {Username}", username);
                    await Clients.Caller.SendAsync("LoadChatHistory", new List<ChatMessage>());
                }

                // Update online users
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
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
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
        }

        // Public message
        public async Task SendMessage(string message)
        {
            var username = GetUsernameFromHttp(Context) ?? "Unknown";

            var userId = Guid.NewGuid().ToString();
            var timestamp = DateTime.UtcNow;

            var chatMessageEntity = new ChatMessage
            {
                UserId = userId,
                UserName = username,
                Message = message,
                Timestamp = timestamp
            };

            await _message_service.SaveMessageAsync(chatMessageEntity);

            var chatMessageDto = new ChatMessageDto
            {
                UserId = userId,
                Username = username,
                Text = message,
                Timestamp = timestamp.ToString("HH:mm:ss")
            };

            _logger.LogInformation("Public Message from {Username}: {Message}", username, message);

            await Clients.All.SendAsync("ReceiveMessage", chatMessageDto.Username, chatMessageDto.Text, chatMessageDto.Timestamp);
            await Clients.AllExcept(Context.ConnectionId).SendAsync("ReceiveNotification", $"{username} sent a message");
        }

        // Private message
        public async Task SendPrivateMessage(string recipientUsername, string message)
        {
            var senderName = GetUsernameFromHttp(Context);
            if (string.IsNullOrEmpty(senderName) || string.IsNullOrEmpty(recipientUsername) || string.IsNullOrEmpty(message))
            {
                await Clients.Caller.SendAsync("MessageError", "Recipient and message cannot be empty.");
                return;
            }

            var recipientConnections = _onlineUserService.GetConnectionsByUsername(recipientUsername);
            if (recipientConnections.Any())
            {
                _logger.LogInformation("Private Message from {Sender} to {Recipient}: {Message}", senderName, recipientUsername, message);

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
                _logger.LogInformation("{Recipient} is not online.", recipientUsername);
            }
        }

        // Add user to online list manually
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

        // Typing indicator
        public async Task SendTypingIndicator()
        {
            var username = GetUsernameFromHttp(Context);
            if (string.IsNullOrEmpty(username)) return;

            _onlineUserService.AddTypingUser(username);
            await Clients.Others.SendAsync("UserTyping", username);
        }

        public async Task StopTypingIndicator()
        {
            var username = GetUsernameFromHttp(Context);
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
            var messages = await _message_service.GetChatHistory();
            await Clients.Caller.SendAsync("ReceiveHistory", messages);
        }
    }
}

