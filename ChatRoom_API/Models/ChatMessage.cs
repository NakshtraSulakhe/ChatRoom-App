namespace ChatRoom_API.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class PrivateMessage
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string ReceiverId { get; set; }
        public string ReceiverName { get; set; }
        public string Message { get; set; }  
        public DateTime Timestamp { get; set; }
    }
}
