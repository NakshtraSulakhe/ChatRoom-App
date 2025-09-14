using System.ComponentModel.DataAnnotations;

namespace ChatRoom_API.DTOs
{
    public class MessageDto
    {
        [Required]
        [StringLength(500, ErrorMessage = "Message cannot exceed 500 characters.")]
        public string Message { get; set; } = string.Empty;
    }

    public class PrivateMessageDto
    {
        [Required]
        public string ReceiverId { get; set; } = string.Empty;

        [Required]
        [StringLength(500, ErrorMessage = "Message cannot exceed 500 characters.")]
        public string Message { get; set; } = string.Empty;
    }
}
