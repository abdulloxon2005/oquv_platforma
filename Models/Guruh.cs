using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace talim_platforma.Models
{
    public class Guruh
    {
        public int Id { get; set; }
        public string Nomi { get; set; }
        public DateTime BoshlanishSanasi { get; set; }
        public string DarsKunlari { get; set; }
        public DateTime DarsVaqti { get; set; }
        public string Xona { get; set; }
        public string Holati { get; set; }

        public int KursId { get; set; }
        public Kurs? Kurs { get; set; }

        public int OqituvchiId { get; set; }
        public Foydalanuvchi? Oqituvchi { get; set; }

        public DateTime YaratilganVaqt { get; set; } = DateTime.Now;
        public DateTime YangilanganVaqt { get; set; } = DateTime.Now;
        public ICollection<Davomat>? Davomatlar { get; set; }
        public ICollection<TalabaGuruh>? TalabaGuruhlar { get; set; }
        public ICollection<Dars>? Darslar { get; set; }
    }

}
