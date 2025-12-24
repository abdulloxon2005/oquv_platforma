using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace talim_platforma.Models
{
    public class TalabaKurs
    {
        public int Id { get; set; }
        public int KursId { get; set; }
        public int TalabaId { get; set; }
        public DateTime QoshilganSana { get; set; }= DateTime.Now;
        public string Holati { get; set; }

        public DateTime YaratilganVaqt { get; set; } = DateTime.Now;
        public DateTime YangilanganVaqt { get; set; } = DateTime.Now;

        public Kurs? Kurs { get; set; }
        public Foydalanuvchi? Talaba { get; set; }
        public bool Aktivmi { get; set; } = true;
    }

}