using System.ComponentModel.DataAnnotations;

namespace StayBnb.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        [Required, StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [EnumDataType(typeof(NotificationType))]
        public NotificationType Type { get; set; }

        public bool IsRead { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
