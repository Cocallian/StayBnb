using System.ComponentModel.DataAnnotations;

namespace StayBnb.Models
{
    public class CheckInViewModel
    {
        [Required]
        public int PropertyId { get; set; }

        [Required, DataType(DataType.Date), Display(Name = "Check-in date")]
        public DateTime? CheckInDate { get; set; }

        [Required, DataType(DataType.Date), Display(Name = "Check-out date")]
        public DateTime? CheckOutDate { get; set; }

        [Required, Range(1, 50, ErrorMessage = "Number of guests must be between 1 and 50.")]
        [Display(Name = "Guests")]
        public int NumberOfGuests { get; set; } = 1;

        [Required(ErrorMessage = "Please choose a document type."), StringLength(100)]
        [Display(Name = "Document type")]
        public string DocumentType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please upload your ID or passport.")]
        [Display(Name = "ID / Passport")]
        public IFormFile? Document { get; set; }

        public HostProperty? Property { get; set; }
    }
}
