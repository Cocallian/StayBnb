using System.ComponentModel.DataAnnotations;

namespace StayBnb.Models
{
    public class Message
    {
        public int Id { get; set; }

        [Required]
        public string SenderId { get; set; } = string.Empty;
        public ApplicationUser? Sender { get; set; }

        [Required]
        public string ReceiverId { get; set; } = string.Empty;
        public ApplicationUser? Receiver { get; set; }

        [Required, StringLength(4000, MinimumLength = 1), DataType(DataType.MultilineText)]
        public string Content { get; set; } = string.Empty;

        [DataType(DataType.DateTime)]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; }
    }
}
