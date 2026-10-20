using System.ComponentModel.DataAnnotations;

namespace StayBnb.Models
{
    public class HostApplication
    {
        public int Id { get; set; }

        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;
        public ApplicationUser? ApplicationUser { get; set; }

        [Required]
        public int PropertyId { get; set; }
        public HostProperty? Property { get; set; }

        [EnumDataType(typeof(ApplicationStatus))]
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;

        [DataType(DataType.DateTime)]
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    }
}
