using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace talim_platforma.Models
{
    public class DavomatSabab
    {
        public int Id { get; set; }
        public int TalabaId { get; set; }
        public int DavomatId { get; set; }
        public string Sabab { get; set; } // Admin yozadi
        public DateTime Sana { get; set; } = DateTime.Now;

        public Foydalanuvchi Talaba { get; set; }
        public Davomat Davomat { get; set; }
    }



}