using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace talim_platforma.Models
{
    public class Kurs
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Kurs nomi kiritilishi shart")]
        public string Nomi { get; set; }
        [Required(ErrorMessage = "Tavsif kiritilishi shart")]
        public string Tavsif { get; set; }
        public string Darajasi { get; set; }
        [Range(0, 1000000, ErrorMessage = "Narx 0 dan katta bo'lishi kerak")]
        public decimal Narxi { get; set; }
        public int Davomiyligi { get; set; } // soatlar
        public string Kategoriya { get; set; }
        public bool Faol { get; set; } = true;

        public DateTime YaratilganVaqt { get; set; } = DateTime.Now;
        public DateTime YangilanganVaqt { get; set; } = DateTime.Now;
        public ICollection<TalabaKurs>? TalabaKurslar { get; set; }
        public ICollection<Guruh>? Guruhlar { get; set; }
    }

}