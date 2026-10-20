using System.ComponentModel.DataAnnotations;

namespace StayBnb.Models
{
    public class CheckInProcess
    {
        public int Id { get; set; }

        [Required]
        public int PropertyId { get; set; }
        public HostProperty? Property { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, DataType(DataType.MultilineText), Display(Name = "Steps (JSON)")]
        public string StepsJson { get; set; } = "[]";

        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
