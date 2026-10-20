using System.ComponentModel.DataAnnotations;

namespace StayBnb.Models
{
    public class GuestDocument
    {
        public int Id { get; set; }

        [Required]
        public int GuestCheckInId { get; set; }
        public GuestCheckIn? GuestCheckIn { get; set; }

        [Required, StringLength(100), Display(Name = "Document type")]
        public string DocumentType { get; set; } = string.Empty;

        [Required, StringLength(200), Display(Name = "File name")]
        public string FileName { get; set; } = string.Empty;

        [EnumDataType(typeof(DocumentStatus))]
        public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    }
}
