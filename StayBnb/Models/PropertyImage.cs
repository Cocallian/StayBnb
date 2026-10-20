using System.ComponentModel.DataAnnotations;

namespace StayBnb.Models
{
    public class PropertyImage
    {
        public int Id { get; set; }

        [Required]
        public int PropertyId { get; set; }
        public HostProperty? Property { get; set; }

        [Required, StringLength(500), Display(Name = "Image URL")]
        public string ImageUrl { get; set; } = string.Empty;
    }
}
