using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace talim_platforma.Models
{
    public class Dars
    {
        public int Id { get; set; }
        public int GuruhId { get; set; }
        public int OqituvchiId { get; set; }

        public DateTime Sana { get; set; }
        public string Mavzu { get; set; }
        public string DarsTuri { get; set; }
        public string Davomiylik { get; set; }

        public DateTime YaratilganVaqt { get; set; } = DateTime.Now;
        public DateTime YangilanganVaqt { get; set; } = DateTime.Now;

        public Guruh Guruh { get; set; }
        public Foydalanuvchi Oqituvchi { get; set; }
    }

}