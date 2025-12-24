using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace talim_platforma.Models
{
    public class FoydalanuvchiToliqMalumoti
    {
        public int Id { get; set; }
        public int FoydalanuvchiId { get; set; }

        public string QarindoshlikDarajasi { get; set; }
        public string QarindoshIsmi { get; set; }
        public string QarindoshFamilyasi { get; set; }
        public string QarindoshTelefonRaqami { get; set; }
        public string Manzil { get; set; }

        public DateTime YaratilganVaqt { get; set; } = DateTime.Now;
        public DateTime YangilanganVaqt { get; set; } = DateTime.Now;

        public Foydalanuvchi Foydalanuvchi { get; set; }
    }

}