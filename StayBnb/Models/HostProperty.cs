using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StayBnb.Models
{
    public class HostProperty
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(2000), DataType(DataType.MultilineText)]
        public string Description { get; set; } = string.Empty;

        [Required, Range(1, 1000000, ErrorMessage = "Price per night must be greater than 0."), DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)"), Display(Name = "Price per night")]
        public decimal PricePerNight { get; set; }

        [Range(0, 1000000), DataType(DataType.Currency), Column(TypeName = "decimal(18,2)"), Display(Name = "Cleaning fee")]
        public decimal CleaningFee { get; set; }

        [Range(0, 1000000), DataType(DataType.Currency), Column(TypeName = "decimal(18,2)"), Display(Name = "Service fee")]
        public decimal ServiceFee { get; set; }

        [Required]
        public string HostId { get; set; } = string.Empty;
        public ApplicationUser? Host { get; set; }

        [Required, StringLength(200)]
        public string Address { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string City { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public ICollection<PropertyImage> Images { get; set; } = new List<PropertyImage>();
        public ICollection<Amenity> Amenities { get; set; } = new List<Amenity>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public CheckInProcess? CheckInProcess { get; set; }
    }
}
