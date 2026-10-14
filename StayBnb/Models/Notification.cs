using System.ComponentModel.DataAnnotations;

namespace StayBnb.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        [Required, StringLength(100)]
        public string Title { get; set; } = string.Empty;

        public NotificationType Type { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
