using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using talim_platforma.Data;
using talim_platforma.Models;
using talim_platforma.Models.ViewModels;
using talim_platforma.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace talim_platforma.Controllers
{
    [Authorize(Roles = "teacher,admin")]
    [AutoValidateAntiforgeryToken]
    public class TolovController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly TelegramBotService _telegramBot; // ✅ interfeys emas, real servis
        private readonly IMobilePushService _pushService;   // ✅ mobil push uchun servis

        public TolovController(ApplicationDbContext context, TelegramBotService telegramBot, IMobilePushService pushService)
        {
            _context = context;
            _telegramBot = telegramBot;
            _pushService = pushService;
        }

        // GET: /Tolov/Qoshish
        [HttpGet]
        public IActionResult Qoshish()
        {
            return View();
        }

        // AJAX: Talaba qidiruv (ismi yoki telefon bo'yicha, case-insensitive)
        [HttpGet]
        public async Task<IActionResult> QidirTalaba(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Json(new object[0]);

            q = q.Trim().ToLower();

            var result = await _context.Foydalanuvchilar
                .Where(f => (f.Rol == "Talaba" || f.Rol.ToLower() == "student") &&
                           (EF.Functions.Like(f.Ism.ToLower(), $"%{q}%") ||
                            EF.Functions.Like(f.Familiya.ToLower(), $"%{q}%") ||
                            EF.Functions.Like(f.TelefonRaqam, $"%{q}%")))
                .Select(f => new { id = f.Id, text = f.Familiya + " " + f.Ism + " (" + f.TelefonRaqam + ")", ism = f.Ism, telefon = f.TelefonRaqam })
                .Take(20)
                .ToListAsync();

            return Json(result);
        }

        // AJAX: Talabaning kurslari
        [HttpGet]
        public async Task<IActionResult> GetKurslarByTalaba(int id)
        {
            var kurslar = await _context.TalabaKurslar
                .Include(tk => tk.Kurs)
                .Where(tk => tk.TalabaId == id && tk.Aktivmi)
                .Select(tk => new { id = tk.Kurs.Id, nomi = tk.Kurs.Nomi, narx = tk.Kurs.Narxi })
                .ToListAsync();

            return Json(kurslar);
        }

        // POST: Qoshish (to'lovni saqlash)
        [HttpPost]
        public async Task<IActionResult> Qoshish(int talabaId, int kursId, decimal miqdor, string tolovUsuli)
        {
            if (talabaId <= 0 || kursId <= 0 || miqdor <= 0 || string.IsNullOrWhiteSpace(tolovUsuli))
            {
                TempData["ErrorMessage"] = "Iltimos, barcha maydonlarni to'ldiring.";
                return RedirectToAction("Qoshish");
            }

            var talaba = await _context.Foydalanuvchilar.FindAsync(talabaId);
            if (talaba == null)
            {
                TempData["ErrorMessage"] = "Talaba topilmadi.";
                return RedirectToAction("Qoshish");
            }

            var kurs = await _context.Kurslar.FindAsync(kursId);
            if (kurs == null)
            {
                TempData["ErrorMessage"] = "Kurs topilmadi.";
                return RedirectToAction("Qoshish");
            }

            var talabaKurs = await _context.TalabaKurslar.FirstOrDefaultAsync(tk => tk.TalabaId == talabaId && tk.KursId == kursId);

            var yangi = new Tolov
            {
                TalabaId = talabaId,
                KursId = kursId,
                Miqdor = miqdor,
                Sana = DateTime.Now,
                TolovUsuli = tolovUsuli,
                TalabaKursId = talabaKurs?.Id
            };

            _context.Tolovlar.Add(yangi);
            await _context.SaveChangesAsync();

            var statusSummary = await RecalculatePaymentStatusAsync(talabaId, kursId);
            var holat = statusSummary.Status;
            var qarzdorlik = statusSummary.Qarzdorlik;
            var haqdorlik = statusSummary.Haqdorlik;

            // 🟢 Telegram xabar yuborish
            if (!string.IsNullOrEmpty(talaba.ChatId))
            {
                try
                {
                    long chatId = Convert.ToInt64(talaba.ChatId);
                    await _telegramBot.SendPaymentNotificationAsync(
                        chatId,
                        $"{talaba.Familiya} {talaba.Ism}",
                        kurs.Nomi,
                        miqdor,
                        DateTime.Now,
                        holat
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Telegram xabar yuborishda xato: {ex.Message}");
                }
            }

            // 🔔 Mobil ilova uchun push yuborish
            try
            {
                // DeviceToken bo'lmasa, topic ishlatamiz
                var deviceToken = $"/topics/user_{talabaId}"; // yoki talaba?.DeviceToken bo'lsa ishlatiladi
                await _pushService.SendPushToUserAsync(
                    deviceToken,
                    "To'lov qabul qilindi",
                    $"{talaba.Familiya} {talaba.Ism} {kurs.Nomi} uchun {miqdor:N0} so'm to'lov qildi. {holat}"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Mobil push yuborishda xato: {ex.Message}");
            }

            TempData["SuccessMessage"] = "To‘lov muvaffaqiyatli saqlandi.";
            TempData["TolovId"] = yangi.Id;

            return RedirectToAction("Chek", new { id = yangi.Id });
        }

        // Chek sahifasi
        public async Task<IActionResult> Chek(int id)
        {
            var tolov = await _context.Tolovlar
                .Include(t => t.Talaba)
                .Include(t => t.Kurs)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tolov == null) return NotFound();
            return View(tolov);
        }

        // Chekni PDF shaklida yuklab olish
        public async Task<IActionResult> ChekYuklabOlish(int id)
        {
            var tolov = await _context.Tolovlar
                .Include(t => t.Talaba)
                .Include(t => t.Kurs)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tolov == null) return NotFound();

            using (var ms = new MemoryStream())
            {
                var doc = new Document(PageSize.A4, 36, 36, 36, 36);
                var writer = PdfWriter.GetInstance(doc, ms);
                doc.Open();

                var fontTitle = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
                var font = FontFactory.GetFont(FontFactory.HELVETICA, 11);

                doc.Add(new Paragraph("TO'LOV CHEKI", fontTitle) { Alignment = Element.ALIGN_CENTER });
                doc.Add(new Paragraph(" "));

                var tbl = new PdfPTable(2) { WidthPercentage = 100 };
                tbl.SetWidths(new float[] { 30f, 70f });

                void AddRow(string left, string right)
                {
                    tbl.AddCell(new PdfPCell(new Phrase(left, font)) { Border = Rectangle.NO_BORDER });
                    tbl.AddCell(new PdfPCell(new Phrase(right, font)) { Border = Rectangle.NO_BORDER });
                }

                AddRow("F.I.Sh:", $"{tolov.Talaba.Familiya} {tolov.Talaba.Ism}");
                AddRow("Telefon:", $"{tolov.Talaba.TelefonRaqam}");
                AddRow("Kurs:", $"{tolov.Kurs.Nomi}");
                AddRow("Summa:", $"{tolov.Miqdor:N0} so'm");
                AddRow("To'lov usuli:", tolov.TolovUsuli);
                AddRow("Sana:", tolov.Sana.ToString("dd.MM.yyyy HH:mm"));
                AddRow("Holat:", tolov.Holat);
                AddRow("Chek raqami:", tolov.ChekRaqami);

                doc.Add(tbl);
                doc.Add(new Paragraph(" "));
                doc.Add(new Paragraph("Rahmat! Sizni biz bilan ko‘rishdan xursandmiz.", font));

                doc.Close();

                var bytes = ms.ToArray();
                return File(bytes, "application/pdf", $"Chek_{tolov.ChekRaqami}.pdf");
            }
        }

        public IActionResult Index()
        {
            const int latestCount = 50; // faqat eng so'nggi 50 ta to'lovni ko'rsatamiz

            var list = _context.Tolovlar
                .Include(t => t.Talaba)
                .Include(t => t.Kurs)
                .OrderByDescending(t => t.Sana)
                .Take(latestCount)
                .ToList();

            return View(list);
        }

        public async Task<IActionResult> Tarix(int id)
        {
            var tarix = await _context.Tolovlar
                .Include(t => t.Kurs)
                .Include(t => t.Talaba)
                .Where(t => t.TalabaId == id)
                .OrderByDescending(t => t.Sana)
                .ToListAsync();

            return View(tarix);
        }

        public IActionResult Malumot(int id)
        {
            var talaba = _context.Foydalanuvchilar.FirstOrDefault(t => t.Id == id);
            if (talaba == null)
                return NotFound();

            var talabaKurslar = _context.TalabaKurslar
                .Include(tk => tk.Kurs)
                .Where(tk => tk.TalabaId == id)
                .ToList();

            var tolovGruplari = _context.Tolovlar
                .Where(t => t.TalabaId == id)
                .GroupBy(t => t.KursId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Miqdor));

            var kursHolati = new List<CoursePaymentStatus>();

            foreach (var tk in talabaKurslar)
            {
                var kursId = tk.KursId;
                var kursNarxi = tk.Kurs?.Narxi ?? 0;
                var tolangan = tolovGruplari.TryGetValue(kursId, out var sum) ? sum : 0;

                kursHolati.Add(new CoursePaymentStatus
                {
                    KursNomi = tk.Kurs?.Nomi ?? "Kurs",
                    KursNarxi = kursNarxi,
                    Tolangan = tolangan,
                    Qarzdorlik = Math.Max(0, kursNarxi - tolangan),
                    Haqdorlik = Math.Max(0, tolangan - kursNarxi)
                });
            }

            var existingCourseIds = talabaKurslar.Select(tk => tk.KursId).ToHashSet();
            var orphans = tolovGruplari.Keys
                .Where(k => !existingCourseIds.Contains(k))
                .ToList();

            var orphanCourses = _context.Kurslar
                .Where(k => orphans.Contains(k.Id))
                .ToDictionary(k => k.Id, k => k);

            foreach (var pair in orphans)
            {
                if (!orphanCourses.TryGetValue(pair, out var kurs))
                    continue;

                var tolangan = tolovGruplari[pair];

                kursHolati.Add(new CoursePaymentStatus
                {
                    KursNomi = kurs.Nomi,
                    KursNarxi = kurs.Narxi,
                    Tolangan = tolangan,
                    Qarzdorlik = Math.Max(0, kurs.Narxi - tolangan),
                    Haqdorlik = Math.Max(0, tolangan - kurs.Narxi)
                });
            }

            var model = new TalabaTolovHolatiViewModel
            {
                Talaba = talaba,
                KursHolati = kursHolati
            };

            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> Ochirish(int id)
        {
            var tolov = await _context.Tolovlar
                .Include(t => t.Talaba)
                .Include(t => t.Kurs)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tolov == null)
                return NotFound();

            return PartialView("_TolovOchirishModal", tolov);
        }

        // 🗑️ O‘chirish POST – Tasdiqlangandan keyin
        [HttpPost]
        public async Task<IActionResult> Ochir(int id)
        {
            var tolov = await _context.Tolovlar.FindAsync(id);
            if (tolov == null)
                return NotFound();

            var talabaId = tolov.TalabaId;
            var kursId = tolov.KursId;

            _context.Tolovlar.Remove(tolov);
            await _context.SaveChangesAsync();

            await RecalculatePaymentStatusAsync(talabaId, kursId);

            return RedirectToAction(nameof(Index));
        }

        private record PaymentStatusSummary(decimal Qarzdorlik, decimal Haqdorlik, string Status)
        {
            public static PaymentStatusSummary Empty { get; } = new(0, 0, "To‘lov kiritilmagan");
        }

        private async Task<PaymentStatusSummary> RecalculatePaymentStatusAsync(int talabaId, int kursId)
        {
            var kurs = await _context.Kurslar
                .Where(k => k.Id == kursId)
                .Select(k => new { k.Id, k.Narxi })
                .FirstOrDefaultAsync();

            if (kurs == null)
            {
                return PaymentStatusSummary.Empty;
            }

            var payments = await _context.Tolovlar
                .Where(t => t.TalabaId == talabaId && t.KursId == kursId)
                .OrderBy(t => t.Sana)
                .ToListAsync();

            if (payments.Count == 0)
            {
                var emptyTalabaKurs = await _context.TalabaKurslar
                    .FirstOrDefaultAsync(tk => tk.TalabaId == talabaId && tk.KursId == kursId);

            if (emptyTalabaKurs != null)
                {
                    emptyTalabaKurs.Holati = PaymentStatusSummary.Empty.Status;
                    emptyTalabaKurs.YangilanganVaqt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }

                return PaymentStatusSummary.Empty;
            }

            decimal cumulative = 0m;
            PaymentStatusSummary summary = PaymentStatusSummary.Empty;

            foreach (var payment in payments)
            {
                cumulative += payment.Miqdor;
                var difference = cumulative - kurs.Narxi;

                payment.Qarzdorlik = difference < 0 ? Math.Abs(difference) : 0m;
                payment.Haqdorlik = difference > 0 ? difference : 0m;

                payment.Holat = payment.Qarzdorlik > 0
                    ? $"Qarzdor ({payment.Qarzdorlik:N0} so‘m)"
                    : payment.Haqdorlik > 0
                        ? $"Haqdor ({payment.Haqdorlik:N0} so‘m)"
                        : "To‘liq to‘langan";

                summary = new PaymentStatusSummary(payment.Qarzdorlik, payment.Haqdorlik, payment.Holat);
            }

            var talabaKurs = await _context.TalabaKurslar
                .FirstOrDefaultAsync(tk => tk.TalabaId == talabaId && tk.KursId == kursId);

            if (talabaKurs != null)
            {
                talabaKurs.Holati = summary.Status;
                talabaKurs.YangilanganVaqt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return summary;
        }
    }
}
