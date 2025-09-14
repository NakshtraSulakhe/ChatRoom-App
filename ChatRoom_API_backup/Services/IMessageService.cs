using ChatRoom_API.Models;

namespace ChatRoom_API.Interface
{
    public interface IMessageService
    {
        Task<List<ChatMessage>> GetMessageHistoryAsync();

        // Add this method for the Hub usage
        Task<List<ChatMessage>> GetChatHistory();
        Task SaveMessageAsync(ChatMessage message);

    }
}
