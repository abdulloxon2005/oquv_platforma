using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using talim_platforma.Data;
using talim_platforma.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;

namespace talim_platforma.Controllers
{
    [Authorize(Roles = "admin")]
    public class HisobotController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HisobotController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🔹 Index sahifasi: navbarda kirimlar va to‘lovlar bo‘limi
        public IActionResult Index()
        {
            return View();
        }

        // 🔹 Bugungi kirim
        public async Task<IActionResult> BugungiKirim()
        {
            var bugun = DateTime.Today;
            var tolovlar = await _context.Tolovlar
                .Include(t => t.Talaba)
                .Where(t => t.Sana.Date == bugun)
                .ToListAsync();

            ViewBag.Jami = tolovlar.Sum(t => t.Miqdor);
            return PartialView("_BugungiKirim", tolovlar);
        }

        // 🔹 Oylik kirim
        public async Task<IActionResult> OylikKirim(int? oy)
        {
            int oylik = oy ?? DateTime.Now.Month;
            var tolovlar = await _context.Tolovlar
                .Include(t => t.Talaba)
                .Where(t => t.Sana.Month == oylik)
                .ToListAsync();

            ViewBag.Oy = oylik;
            ViewBag.Jami = tolovlar.Sum(t => t.Miqdor);
            return PartialView("_OylikKirim", tolovlar);
        }

        // 🔹 Yillik kirim
        public async Task<IActionResult> YillikKirim(int? yil)
        {
            int yilTanlangan = yil ?? DateTime.Now.Year;
            var tolovlar = await _context.Tolovlar
                .Include(t => t.Talaba)
                .Where(t => t.Sana.Year == yilTanlangan)
                .ToListAsync();

            ViewBag.Yil = yilTanlangan;
            ViewBag.Jami = tolovlar.Sum(t => t.Miqdor);
            return PartialView("_YillikKirim", tolovlar);
        }

        // 🔹 PDF shaklida yuklab olish
        public async Task<IActionResult> YuklabOlish(string turi, int? oy, int? yil)
        {
            // Qarzdor talabalar uchun alohida PDF (qarzdorlik hisobot)
            if (turi == "qarzdor")
            {
                var qarzdorlar = await _context.Tolovlar
                    .Where(t => t.Qarzdorlik > 0)
                    .Where(t =>
                        t.Sana == _context.Tolovlar
                            .Where(x => x.TalabaId == t.TalabaId && x.KursId == t.KursId)
                            .Max(x => x.Sana))
                    .Include(t => t.Talaba)
                    .Include(t => t.Kurs)
                    .AsNoTracking()
                    .OrderByDescending(t => t.Sana)
                    .ToListAsync();

                using (MemoryStream ms = new MemoryStream())
                {
                    Document doc = new Document(PageSize.A4, 25, 25, 25, 25);
                    PdfWriter.GetInstance(doc, ms);
                    doc.Open();

                    string sarlavhaQ = "Qarzdor talabalar hisobot";
                    doc.Add(new Paragraph(sarlavhaQ) { Alignment = Element.ALIGN_CENTER, SpacingAfter = 10f });

                    PdfPTable tableQ = new PdfPTable(3) { WidthPercentage = 100 };
                    tableQ.AddCell("Talaba");
                    tableQ.AddCell("Kurs");
                    tableQ.AddCell("Qarzdorlik (so‘m)");

                    foreach (var t in qarzdorlar)
                    {
                        tableQ.AddCell($"{t.Talaba?.Familiya} {t.Talaba?.Ism}");
                        tableQ.AddCell(t.Kurs?.Nomi ?? "-");
                        tableQ.AddCell(t.Qarzdorlik.ToString("N0"));
                    }

                    doc.Add(tableQ);
                    doc.Add(new Paragraph($"\nJami qarzdorlik: {qarzdorlar.Sum(t => t.Qarzdorlik):N0} so‘m")
                    { Alignment = Element.ALIGN_RIGHT });

                    doc.Close();
                    byte[] bytesQ = ms.ToArray();
                    return File(bytesQ, "application/pdf", "qarzdor_talabalar.pdf");
                }
            }

            // Kirim hisobotlari (kunlik / oylik / yillik)
            IEnumerable<Tolov> tolovlar = new List<Tolov>();
            string sarlavha = "Kirim hisobot";

            switch (turi)
            {
                case "kunlik":
                    tolovlar = await _context.Tolovlar.Include(t => t.Talaba)
                        .Where(t => t.Sana.Date == DateTime.Today).ToListAsync();
                    sarlavha += " (Bugungi)";
                    break;
                case "oylik":
                    int oylik = oy ?? DateTime.Now.Month;
                    tolovlar = await _context.Tolovlar.Include(t => t.Talaba)
                        .Where(t => t.Sana.Month == oylik).ToListAsync();
                    sarlavha += $" ({oylik}-oy)";
                    break;
                case "yillik":
                    int yilTanlangan = yil ?? DateTime.Now.Year;
                    tolovlar = await _context.Tolovlar.Include(t => t.Talaba)
                        .Where(t => t.Sana.Year == yilTanlangan).ToListAsync();
                    sarlavha += $" ({yilTanlangan}-yil)";
                    break;
            }

            using (MemoryStream ms = new MemoryStream())
            {
                Document doc = new Document(PageSize.A4, 25, 25, 25, 25);
                PdfWriter.GetInstance(doc, ms);
                doc.Open();

                doc.Add(new Paragraph(sarlavha) { Alignment = Element.ALIGN_CENTER, SpacingAfter = 10f });

                PdfPTable table = new PdfPTable(3) { WidthPercentage = 100 };
                table.AddCell("Talaba");
                table.AddCell("Miqdor (so‘m)");
                table.AddCell("Sana");

                foreach (var t in tolovlar)
                {
                    table.AddCell($"{t.Talaba.Familiya} {t.Talaba.Ism}");
                    table.AddCell(t.Miqdor.ToString("N0"));
                    table.AddCell(t.Sana.ToString("dd.MM.yyyy"));
                }

                doc.Add(table);
                doc.Add(new Paragraph($"\nJami: {tolovlar.Sum(t => t.Miqdor):N0} so‘m") { Alignment = Element.ALIGN_RIGHT });

                doc.Close();
                byte[] bytes = ms.ToArray();
                return File(bytes, "application/pdf", $"{turi}_hisobot.pdf");
            }
        }

        public async Task<IActionResult> QarzdorTalabalar()
        {
            var qarzdorlar = await _context.Tolovlar
                .Where(t => t.Qarzdorlik > 0)
                .Where(t =>
                    t.Sana == _context.Tolovlar
                        .Where(x => x.TalabaId == t.TalabaId && x.KursId == t.KursId)
                        .Max(x => x.Sana))
                .Include(t => t.Talaba)
                .Include(t => t.Kurs)
                .AsNoTracking()
                .OrderByDescending(t => t.Sana)
                .ToListAsync();

            ViewBag.Jami = qarzdorlar.Sum(t => t.Qarzdorlik);

            return PartialView("_QarzdorTalabalar", qarzdorlar);
        }


    }
}
