using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace StayBnb.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required, StringLength(50), Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50), Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(500), Url, Display(Name = "Profile picture")]
        public string? ProfilePictureUrl { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<HostProperty> Properties { get; set; } = new List<HostProperty>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
