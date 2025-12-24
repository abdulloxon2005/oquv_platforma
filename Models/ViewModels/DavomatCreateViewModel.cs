using System.ComponentModel.DataAnnotations;

namespace talim_platforma.Models.ViewModels
{
    public class DavomatCreateViewModel
    {
        public int? Id { get; set; }

        [Required]
        public int GuruhId { get; set; }

        [Required]
        public int TalabaId { get; set; }

        [Required]
        public int DarsId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Sana { get; set; } = DateTime.Today;

        [Required]
        public string Holati { get; set; }

        public string? Izoh { get; set; }
    }
}

