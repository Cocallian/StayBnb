using System.ComponentModel.DataAnnotations;

namespace StayBnb.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        public int PropertyId { get; set; }
        public HostProperty? Property { get; set; }

        [Required]
        public string ReviewerId { get; set; } = string.Empty;
        public ApplicationUser? Reviewer { get; set; }

        [Required, Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        [StringLength(1000)]
        public string Comment { get; set; } = string.Empty;

        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
