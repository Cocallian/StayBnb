using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StayBnb.Models
{
    public class Booking
    {
        public int Id { get; set; }

        [Required]
        public int PropertyId { get; set; }
        public HostProperty? Property { get; set; }

        [Required]
        public string GuestId { get; set; } = string.Empty;
        public ApplicationUser? Guest { get; set; }

        [Required, DataType(DataType.Date), Display(Name = "Check-in")]
        public DateTime CheckInDate { get; set; }

        [Required, DataType(DataType.Date), Display(Name = "Check-out")]
        public DateTime CheckOutDate { get; set; }

        [Required, Range(1, 50, ErrorMessage = "Number of guests must be between 1 and 50.")]
        [Display(Name = "Guests")]
        public int NumberOfGuests { get; set; }

        [Range(0, 100000000), DataType(DataType.Currency), Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [EnumDataType(typeof(BookingStatus))]
        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        public Payment? Payment { get; set; }
        public GuestCheckIn? GuestCheckIn { get; set; }
    }
}
