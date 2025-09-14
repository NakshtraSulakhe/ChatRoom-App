using ChatRoom_API.Models;
using ChatRoom_API.Data;
using ChatRoom_API.Interface;
using Microsoft.EntityFrameworkCore;

namespace ChatRoom_API.Service
{
    public class MessageService : IMessageService
    {
        private readonly ApplicationDbContext _dbContext;

        public MessageService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // For API usage
        public async Task<List<ChatMessage>> GetMessageHistoryAsync()
        {
            return await _dbContext.ChatMessages
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }

        // For SignalR Hub
        public async Task<List<ChatMessage>> GetChatHistory()
        {
            return await _dbContext.ChatMessages
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }
        public async Task SaveMessageAsync(ChatMessage message)
        {
            _dbContext.ChatMessages.Add(message);
            await _dbContext.SaveChangesAsync();
        }
    }
}
