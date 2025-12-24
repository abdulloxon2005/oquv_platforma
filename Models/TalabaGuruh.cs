using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace talim_platforma.Models
{
    public class TalabaGuruh
    {
        public int Id { get; set; }
        public int GuruhId { get; set; }
        public int TalabaId { get; set; }
        public DateTime QoshilganSana { get; set; }

        public DateTime YaratilganVaqt { get; set; } = DateTime.Now;
        public DateTime YangilanganVaqt { get; set; } = DateTime.Now;

        public Guruh Guruh { get; set; }
        public Foydalanuvchi Talaba { get; set; }
    }

}
