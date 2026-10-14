using System.ComponentModel.DataAnnotations;

namespace StayBnb.Models
{
    public class CheckInProcess
    {
        public int Id { get; set; }
        public int PropertyId { get; set; }
        public HostProperty? Property { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string StepsJson { get; set; } = "[]";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
