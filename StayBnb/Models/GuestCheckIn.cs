using System.ComponentModel.DataAnnotations;

namespace StayBnb.Models
{
    public class GuestCheckIn
    {
        public int Id { get; set; }

        [Required]
        public int BookingId { get; set; }
        public Booking? Booking { get; set; }

        [Required]
        public int CheckInProcessId { get; set; }
        public CheckInProcess? CheckInProcess { get; set; }

        [EnumDataType(typeof(CheckInStatus))]
        public CheckInStatus Status { get; set; } = CheckInStatus.NotStarted;

        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<GuestDocument> Documents { get; set; } = new List<GuestDocument>();
    }
}
