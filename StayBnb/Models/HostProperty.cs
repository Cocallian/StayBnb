using System.ComponentModel.DataAnnotations;

namespace StayBnb.Models
{
    public class HostProperty
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public decimal PricePerNight { get; set; }
        public decimal CleaningFee { get; set; }
        public decimal ServiceFee { get; set; }

        [Required]
        public string HostId { get; set; } = string.Empty;
        public ApplicationUser? Host { get; set; }

        [Required]
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
