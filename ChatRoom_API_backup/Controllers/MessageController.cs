using ChatRoom_API.Data;
using ChatRoom_API.Hubs;
using ChatRoom_API.Models;
using ChatRoom_API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using ChatRoom_API.Interfecae;
using ChatRoom_API.Service;

namespace ChatRoom_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class MessageController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHubContext<ChatHub> _chatHub;
        private readonly IOnlineUserService _onlineUserService;


        public MessageController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IHubContext<ChatHub> chatHub, IOnlineUserService onlineUserService)
        {
            _context = context;
            _userManager = userManager;
            _chatHub = chatHub;
            _onlineUserService = onlineUserService;

        }

        [HttpGet("online-users")]
        public IActionResult GetOnlineUsers()
        {
            var onlineUsers = _onlineUserService.GetOnlineUsers();
            return Ok(onlineUsers);
        }




        /// ✅ **Get public chat history**
        [HttpGet("history")]
        public async Task<ActionResult<List<ChatMessage>>> GetMessageHistory()
        {
            var messages = await _context.ChatMessages
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
            return Ok(messages);
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] MessageDto messageDto)
        {
            if (messageDto == null || string.IsNullOrEmpty(messageDto.Message))
                return BadRequest(new { error = "Message cannot be empty." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "Invalid user token." });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Unauthorized(new { error = "User not found." });

            var message = new ChatMessage
            {
                UserId = userId,
                UserName = user.UserName,
                Message = messageDto.Message,
                Timestamp = DateTime.UtcNow
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            await _chatHub.Clients.All.SendAsync("ReceiveMessage", message.UserName, message.Message, message.Timestamp);
            return Ok(new { message = "Message sent successfully!", data = message });
        }

        [HttpPost("send-private")]
        public async Task<IActionResult> SendPrivateMessage([FromBody] PrivateMessageDto privateMessageDto)
        {
            if (privateMessageDto == null || string.IsNullOrEmpty(privateMessageDto.Message))
                return BadRequest(new { error = "Message cannot be empty." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "Invalid user token." });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Unauthorized(new { error = "User not found." });

            var receiver = await _userManager.FindByIdAsync(privateMessageDto.ReceiverId);
            if (receiver == null)
                return NotFound(new { error = "Receiver not found." });

            var privateMessage = new PrivateMessage
            {
                UserId = userId,
                UserName = user.UserName,
                ReceiverId = privateMessageDto.ReceiverId,
                ReceiverName = receiver.UserName,
                Message = privateMessageDto.Message,
                Timestamp = DateTime.UtcNow
            };

            _context.PrivateMessages.Add(privateMessage);
            await _context.SaveChangesAsync();

            await _chatHub.Clients.User(privateMessageDto.ReceiverId).SendAsync(
                "ReceivePrivateMessage", privateMessage.UserName, privateMessage.Message, privateMessage.Timestamp
            );

            return Ok(new { message = "Private message sent successfully!", data = privateMessage });
        }

        [HttpGet("private-history/{receiverId}")]
        public async Task<IActionResult> GetPrivateChatHistory(string receiverId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "Invalid user token." });

            var messages = await _context.PrivateMessages
                .Where(m => (m.UserId == userId && m.ReceiverId == receiverId) ||
                            (m.UserId == receiverId && m.ReceiverId == userId))
                .OrderBy(m => m.Timestamp)
                .Select(m => new { m.UserName, m.Message, m.Timestamp })
                .ToListAsync();

            return Ok(messages);
        }
    }
}
