using Microsoft.EntityFrameworkCore;
using talim_platforma.Models;

namespace talim_platforma.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // 📘 Foydalanuvchilar va ular haqidagi ma’lumotlar
        public DbSet<Foydalanuvchi> Foydalanuvchilar { get; set; }
        public DbSet<FoydalanuvchiToliqMalumoti> FoydalanuvchiToliqMalumotlari { get; set; }

        // 📚 Kurslar va guruhlar
        public DbSet<Kurs> Kurslar { get; set; }
        public DbSet<Guruh> Guruhlar { get; set; }

        // 👩‍🎓 Talaba aloqalari
        public DbSet<TalabaGuruh> TalabaGuruhlar { get; set; }
        public DbSet<TalabaKurs> TalabaKurslar { get; set; }

        // 🧾 Darslar, baholar, davomat
        public DbSet<Dars> Darslar { get; set; }
        public DbSet<Davomat> Davomatlar { get; set; }
        public DbSet<DavomatSabab> DavomatSabablar { get; set; }
        public DbSet<Baho> Baholar { get; set; }

        // 💳 To‘lovlar
        public DbSet<Tolov> Tolovlar { get; set; }
        public DbSet<Qurilma> Qurilmalar { get; set; }

        // 📢 Reklamalar
        public DbSet<Advertisement> Advertisements { get; set; }


        // 🧠 Imtihonlar
        public DbSet<Imtihon> Imtihonlar { get; set; }
        public DbSet<ImtihonSavol> ImtihonSavollar { get; set; }
        public DbSet<ImtihonNatija> ImtihonNatijalar { get; set; }
        public DbSet<Sertifikat> Sertifikatlar { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1️⃣ Foydalanuvchi ↔️ FoydalanuvchiToliqMalumoti (1:1)
            modelBuilder.Entity<Foydalanuvchi>()
                .HasOne(f => f.FoydalanuvchiToliqMalumoti)
                .WithOne(m => m.Foydalanuvchi)
                .HasForeignKey<FoydalanuvchiToliqMalumoti>(m => m.FoydalanuvchiId)
                .OnDelete(DeleteBehavior.Cascade);

            // 2️⃣ Kurs ↔️ Guruh (1:ko‘p)
            modelBuilder.Entity<Guruh>()
                .HasOne(g => g.Kurs)
                .WithMany(k => k.Guruhlar)
                .HasForeignKey(g => g.KursId)
                .OnDelete(DeleteBehavior.Cascade);

            // 3️⃣ Guruh ↔️ O‘qituvchi (Foydalanuvchi)
            modelBuilder.Entity<Guruh>()
                .HasOne(g => g.Oqituvchi)
                .WithMany()
                .HasForeignKey(g => g.OqituvchiId)
                .OnDelete(DeleteBehavior.Restrict);

            // 4️⃣ Talaba ↔️ Guruh (ko‘p:ko‘p)
            modelBuilder.Entity<TalabaGuruh>()
                .HasOne(tg => tg.Guruh)
                .WithMany(g => g.TalabaGuruhlar)
                .HasForeignKey(tg => tg.GuruhId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TalabaGuruh>()
                .HasOne(tg => tg.Talaba)
                .WithMany()
                .HasForeignKey(tg => tg.TalabaId)
                .OnDelete(DeleteBehavior.Cascade);

            // 5️⃣ Talaba ↔️ Kurs (ko‘p:ko‘p)
            modelBuilder.Entity<TalabaKurs>()
                .HasOne(tk => tk.Kurs)
                .WithMany()
                .HasForeignKey(tk => tk.KursId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TalabaKurs>()
                .HasOne(tk => tk.Talaba)
                .WithMany()
                .HasForeignKey(tk => tk.TalabaId)
                .OnDelete(DeleteBehavior.Cascade);

            // 6️⃣ Dars ↔️ Guruh ↔️ O‘qituvchi
            modelBuilder.Entity<Dars>()
                .HasOne(d => d.Guruh)
                .WithMany(g => g.Darslar)
                .HasForeignKey(d => d.GuruhId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Dars>()
                .HasOne(d => d.Oqituvchi)
                .WithMany()
                .HasForeignKey(d => d.OqituvchiId)
                .OnDelete(DeleteBehavior.Restrict);

            // 7️⃣ Davomat ↔️ Dars ↔️ Talaba
           // Davomat ↔ Guruh
            modelBuilder.Entity<Davomat>()
                .HasOne(d => d.Guruh)
                .WithMany(g => g.Davomatlar)
                .HasForeignKey(d => d.GuruhId)
                .OnDelete(DeleteBehavior.Restrict);

            // Davomat ↔ Talaba
            modelBuilder.Entity<Davomat>()
                .HasOne(d => d.Talaba)
                .WithMany(t => t.TalabaDavomatlar)
                .HasForeignKey(d => d.TalabaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Davomat ↔ O‘qituvchi
            modelBuilder.Entity<Davomat>()
                .HasOne(d => d.Oqituvchi)
                .WithMany(o => o.OqituvchiDavomatlar)
                .HasForeignKey(d => d.OqituvchiId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DavomatSabab>()
                .HasOne(ds => ds.Talaba)
                .WithMany()
                .HasForeignKey(ds => ds.TalabaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DavomatSabab>()
                .HasOne(ds => ds.Davomat)
                .WithMany()
                .HasForeignKey(ds => ds.DavomatId)
                .OnDelete(DeleteBehavior.Cascade);


            // 8️⃣ Baho ↔️ Dars yoki Imtihon
            modelBuilder.Entity<Baho>()
                .HasOne(b => b.Dars)
                .WithMany()
                .HasForeignKey(b => b.DarsId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Baho>()
                .HasOne(b => b.Imtihon)
                .WithMany()
                .HasForeignKey(b => b.ImtihonId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Baho>()
                .HasOne(b => b.Talaba)
                .WithMany()
                .HasForeignKey(b => b.TalabaId)
                .OnDelete(DeleteBehavior.Cascade);

            // 9️⃣ To‘lov ↔️ Talaba ↔️ Kurs
            modelBuilder.Entity<Tolov>()
                .HasOne(t => t.Talaba)
                .WithMany()
                .HasForeignKey(t => t.TalabaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Tolov>()
                .HasOne(t => t.Kurs)
                .WithMany()
                .HasForeignKey(t => t.KursId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔟 Imtihon ↔️ Guruh ↔️ O‘qituvchi
            modelBuilder.Entity<Imtihon>()
                .HasOne(i => i.Guruh)
                .WithMany()
                .HasForeignKey(i => i.GuruhId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Imtihon>()
                .HasOne(i => i.Oqituvchi)
                .WithMany()
                .HasForeignKey(i => i.OqituvchiId)
                .OnDelete(DeleteBehavior.Restrict);

            // 11️⃣ Imtihon ↔️ Savollar
            modelBuilder.Entity<ImtihonSavol>()
                .HasOne(s => s.Imtihon)
                .WithMany(i => i.Savollar)
                .HasForeignKey(s => s.ImtihonId)
                .OnDelete(DeleteBehavior.Cascade);

            // 12️⃣ Imtihon ↔️ Natijalar
            modelBuilder.Entity<ImtihonNatija>()
                .HasOne(n => n.Imtihon)
                .WithMany()
                .HasForeignKey(n => n.ImtihonId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ImtihonNatija>()
                .HasOne(n => n.Talaba)
                .WithMany()
                .HasForeignKey(n => n.TalabaId)
                .OnDelete(DeleteBehavior.Cascade);

            // 13️⃣ Sertifikat ↔️ Talaba ↔️ Imtihon ↔️ Kurs
            modelBuilder.Entity<Sertifikat>()
                .HasOne(s => s.Talaba)
                .WithMany()
                .HasForeignKey(s => s.TalabaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Sertifikat>()
                .HasOne(s => s.Imtihon)
                .WithMany()
                .HasForeignKey(s => s.ImtihonId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Sertifikat>()
                .HasOne(s => s.Kurs)
                .WithMany()
                .HasForeignKey(s => s.KursId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
