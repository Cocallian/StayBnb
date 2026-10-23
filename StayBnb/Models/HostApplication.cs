using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StayBnb.Models
{
    public class HostApplication
    {
        public int Id { get; set; }

        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;
        public ApplicationUser? ApplicationUser { get; set; }

        public int? PropertyId { get; set; }
        public HostProperty? Property { get; set; }

        [EnumDataType(typeof(ApplicationStatus))]
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;

        [DataType(DataType.DateTime)]
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

        [Required, StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(2000), DataType(DataType.MultilineText)]
        public string Description { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string Address { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required, Range(1, 1000000, ErrorMessage = "Price per night must be greater than 0."), DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)"), Display(Name = "Price per night")]
        public decimal PricePerNight { get; set; }

        [Range(0, 1000000), DataType(DataType.Currency), Column(TypeName = "decimal(18,2)"), Display(Name = "Cleaning fee")]
        public decimal CleaningFee { get; set; }

        [Range(0, 1000000), DataType(DataType.Currency), Column(TypeName = "decimal(18,2)"), Display(Name = "Service fee")]
        public decimal ServiceFee { get; set; }

        [StringLength(500), Display(Name = "Image URL")]
        public string ImageUrl { get; set; } = string.Empty;

        [StringLength(500), Display(Name = "Document URL")]
        public string DocumentUrl { get; set; } = string.Empty;

        public ICollection<Amenity> Amenities { get; set; } = new List<Amenity>();
    }
}
