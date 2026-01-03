using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace talim_platforma.Models
{
    public class Davomat
    {
        public int Id { get; set; }

        // 🔗 Aloqalar
        public int DarsId { get; set; }
        public Dars Dars { get; set; }

        public int GuruhId { get; set; }
        public Guruh Guruh { get; set; }

        public int TalabaId { get; set; }
        public Foydalanuvchi Talaba { get; set; }

        public int OqituvchiId { get; set; }
        public Foydalanuvchi Oqituvchi { get; set; }

        // 📅 Dars sanasi
        public DateTime Sana { get; set; }

        // 📋 Holat: "Keldi", "Kelmadi", "Kech keldi", "Uzrli"
        public string Holati { get; set; }

        // 🗒 Izoh (ixtiyoriy)
        public string? Izoh { get; set; }

        // 📊 Baho foizi (faqat "Keldi" holatidagi o'quvchilar uchun): 10, 20, 30... 100
        [Column(TypeName = "decimal(5,2)")]
        public decimal? BahoFoiz { get; set; }

        // ⏱ Yaratilgan va yangilangan vaqtlar
        public DateTime YaratilganVaqt { get; set; } = DateTime.Now;
        public DateTime YangilanganVaqt { get; set; } = DateTime.Now;
    }


}