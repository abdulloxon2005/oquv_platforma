using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using talim_platforma.Data;
using talim_platforma.Models;
using talim_platforma.Models.ViewModels;
using talim_platforma.ViewModels;
using Microsoft.AspNetCore.Identity;
using talim_platforma.Helpers;

namespace talim_platforma.Controllers
{
    [Authorize(Roles = "admin")]
    [AutoValidateAntiforgeryToken]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<Foydalanuvchi> _passwordHasher;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<Foydalanuvchi>();
        }

        // 📊 Asosiy bosh sahifa
        public IActionResult Index()
        {
            var foydalanuvchilar = _context.Foydalanuvchilar.ToList();
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var arxivlanganlar = _context.Foydalanuvchilar
                .Where(f => f.ArxivlanganSana.HasValue)
                .OrderByDescending(f => f.ArxivlanganSana)
                .Take(10)
                .ToList();

            var model = new AdminDashboardViewModel
            {
                TotalStudents = foydalanuvchilar.Count(f => RoleHelper.IsStudent(f.Rol)),
                TotalTeachers = foydalanuvchilar.Count(f => RoleHelper.IsTeacher(f.Rol)),
                TotalCourses = _context.Kurslar.Count(),
                ActiveGroups = _context.Guruhlar.Count(g => g.Holati == "Faol"),
                KursApplicationsCount = _context.KursApplications.Count(a => a.Faol),
                TodaysIncome = _context.Tolovlar.Where(t => t.Sana.Date == today).Sum(t => (decimal?)t.Miqdor) ?? 0,
                MonthlyIncome = _context.Tolovlar.Where(t => t.Sana >= monthStart && t.Sana <= today).Sum(t => (decimal?)t.Miqdor) ?? 0,
                UpcomingExams = _context.Imtihonlar.Count(i => i.Sana >= today),
                RecentPayments = _context.Tolovlar
                    .Include(t => t.Talaba)
                    .Include(t => t.Kurs)
                    .OrderByDescending(t => t.Sana)
                    .Take(6)
                    .ToList(),
                RecentGroups = _context.Guruhlar
                    .Include(g => g.Kurs)
                    .OrderByDescending(g => g.YaratilganVaqt)
                    .Take(4)
                    .ToList(),
                RecentKursApplications = _context.KursApplications
                    .Include(a => a.Kurs)
                    .Where(a => a.Faol)
                    .OrderByDescending(a => a.Sana)
                    .Take(5)
                    .ToList()
            };

            ViewBag.Arxivlanganlar = arxivlanganlar;
            return View(model);
        }

        // 📝 Kursga yozilganlar ro'yxati
        public async Task<IActionResult> KursApplications()
        {
            var applications = await _context.KursApplications
                .Include(a => a.Kurs)
                .Where(a => a.Faol)
                .OrderByDescending(a => a.Sana)
                .ToListAsync();
            
            return View(applications);
        }

        // 📋 Imtihon natijalari (admin uchun umumiy ko‘rinish)
        public async Task<IActionResult> ImtihonNatijalari(int? guruhId, int? imtihonId)
        {
            var query = _context.ImtihonNatijalar
                .Include(n => n.Talaba)
                .Include(n => n.Imtihon)
                    .ThenInclude(i => i.Guruh)
                        .ThenInclude(g => g.Kurs)
                .AsQueryable();

            if (imtihonId.HasValue)
            {
                query = query.Where(n => n.ImtihonId == imtihonId.Value);
            }

            if (guruhId.HasValue)
            {
                query = query.Where(n => n.Imtihon.GuruhId == guruhId.Value);
            }

            var natijalar = await query
                .OrderByDescending(n => n.Sana)
                .ToListAsync();

            ViewBag.Guruhlar = _context.Guruhlar
                .Include(g => g.Kurs)
                .OrderBy(g => g.Nomi)
                .ToList();

            ViewBag.Imtihonlar = _context.Imtihonlar
                .Include(i => i.Guruh)
                .OrderByDescending(i => i.Sana)
                .Take(200)
                .ToList();

            ViewBag.GuruhId = guruhId;
            ViewBag.ImtihonId = imtihonId;
            ViewBag.NatijaManualStats = natijalar.ToDictionary(
                n => n.Id,
                n => ManualResultHelper.TryParse(n.JavoblarJson));

            return View(natijalar);
        }

        // 👥 Foydalanuvchilar ro‘yxati
        public IActionResult Foydalanuvchilar(string? q)
        {
            var query = _context.Foydalanuvchilar
                .Where(f => !f.ArxivlanganSana.HasValue) // Faqat arxivlanmagan foydalanuvchilar
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(f =>
                    (f.Ism != null && f.Ism.ToLower().Contains(term)) ||
                    (f.Familiya != null && f.Familiya.ToLower().Contains(term)) ||
                    (f.Login != null && f.Login.ToLower().Contains(term)) ||
                    (f.TelefonRaqam != null && f.TelefonRaqam.ToLower().Contains(term)));
            }

            ViewBag.QidirilganIsm = q;

            var foydalanuvchilar = query
                .OrderBy(f => f.Familiya)
                .ThenBy(f => f.Ism)
                .ToList();
            return View(foydalanuvchilar);
        }

        // ✅ Yangi foydalanuvchi qo‘shish (GET)
        [HttpGet]
        public IActionResult FoydalanuvchiQoshish()
        {
            return View();
        }

        // ✅ Yangi foydalanuvchi qo‘shish (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FoydalanuvchiQoshish(Foydalanuvchi foydalanuvchi)
        {
            if (!ModelState.IsValid)
                return View(foydalanuvchi);

            // 🔎 Login unikal bo‘lishi kerak (case-insensitive)
            var mavjud = _context.Foydalanuvchilar
                .FirstOrDefault(x => x.Login.ToLower() == foydalanuvchi.Login.ToLower().Trim());
            if (mavjud != null)
            {
                ModelState.AddModelError("Login", "Bu login allaqachon mavjud!");
                return View(foydalanuvchi);
            }

            // 🎭 Rolni standartlashtirish
            foydalanuvchi.Rol = RoleHelper.Normalize(foydalanuvchi.Rol);

            // 🔐 Parolni hash qilish
            foydalanuvchi.Parol = _passwordHasher.HashPassword(foydalanuvchi, foydalanuvchi.Parol);
            foydalanuvchi.YaratilganVaqt = DateTime.Now;
            foydalanuvchi.YangilanganVaqt = DateTime.Now;

            _context.Foydalanuvchilar.Add(foydalanuvchi);
            _context.SaveChanges();

            TempData["success"] = "✅ Foydalanuvchi muvaffaqiyatli qo‘shildi!";
            return RedirectToAction("Foydalanuvchilar");
        }

        // ✏️ Tahrirlash (GET)
        [HttpGet]
        public async Task<IActionResult> FoydalanuvchiTahrirlash(int id)
        {
            var foydalanuvchi = await _context.Foydalanuvchilar.FindAsync(id);
            if (foydalanuvchi == null)
                return NotFound();

            var model = new FoydalanuvchiTahrirlashViewModel
            {
                Id = foydalanuvchi.Id,
                Ism = foydalanuvchi.Ism,
                Familiya = foydalanuvchi.Familiya,
                OtasiningIsmi = foydalanuvchi.OtasiningIsmi,
                TelefonRaqam = foydalanuvchi.TelefonRaqam,
                Login = foydalanuvchi.Login,
                Rol = foydalanuvchi.Rol
            };

            return View(model);
        }

        // ✏️ Tahrirlash (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FoydalanuvchiTahrirlash(int id, FoydalanuvchiTahrirlashViewModel model)
        {
            if (id != model.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            var mavjud = await _context.Foydalanuvchilar.FirstOrDefaultAsync(x => x.Id == id);
            if (mavjud == null)
                return NotFound();

            // 🧩 Ma'lumotlarni yangilash
            mavjud.Ism = model.Ism;
            mavjud.Familiya = model.Familiya;
            mavjud.OtasiningIsmi = model.OtasiningIsmi;
            mavjud.TelefonRaqam = model.TelefonRaqam;
            // 🎭 Rolni standartlashtirish
            mavjud.Rol = RoleHelper.Normalize(model.Rol);
            mavjud.YangilanganVaqt = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(model.Login))
                mavjud.Login = model.Login.Trim();

            // 🔑 Agar parol kiritilgan bo‘lsa — yangilaymiz (hash bilan)
            if (!string.IsNullOrWhiteSpace(model.Parol))
                mavjud.Parol = _passwordHasher.HashPassword(mavjud, model.Parol);

            _context.Update(mavjud);
            await _context.SaveChangesAsync();

            TempData["xabar"] = "✅ Foydalanuvchi muvaffaqiyatli yangilandi!";
            return RedirectToAction("Foydalanuvchilar");
        }

        // ❌ Foydalanuvchini o‘chirish
        public IActionResult FoydalanuvchiOchirish(int id)
        {
            var user = _context.Foydalanuvchilar.Find(id);
            if (user != null)
            {
                _context.Foydalanuvchilar.Remove(user);
                _context.SaveChanges();
            }
            return RedirectToAction("Foydalanuvchilar");
        }

        // 📦 Foydalanuvchini arxivlash
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FoydalanuvchiArxivlash(int id)
        {
            var foydalanuvchi = await _context.Foydalanuvchilar.FindAsync(id);
            if (foydalanuvchi == null)
            {
                TempData["xabar"] = "❌ Foydalanuvchi topilmadi!";
                return RedirectToAction("Foydalanuvchilar");
            }

            if (foydalanuvchi.Arxivlanganmi)
            {
                TempData["xabar"] = "⚠️ Bu foydalanuvchi allaqachon arxivlangan!";
                return RedirectToAction("Foydalanuvchilar");
            }

            foydalanuvchi.ArxivlanganSana = DateTime.Now;
            foydalanuvchi.Faolmi = false; // Arxivlangan foydalanuvchi faol emas
            foydalanuvchi.YangilanganVaqt = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["xabar"] = $"✅ {foydalanuvchi.Familiya} {foydalanuvchi.Ism} muvaffaqiyatli arxivlandi!";
            return RedirectToAction("Foydalanuvchilar");
        }

        // 📤 Foydalanuvchini arxivdan ochish
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FoydalanuvchiArxivdanOchish(int id)
        {
            var foydalanuvchi = await _context.Foydalanuvchilar.FindAsync(id);
            if (foydalanuvchi == null)
            {
                TempData["xabar"] = "❌ Foydalanuvchi topilmadi!";
                return RedirectToAction("Arxivlanganlar");
            }

            if (!foydalanuvchi.Arxivlanganmi)
            {
                TempData["xabar"] = "⚠️ Bu foydalanuvchi arxivlangan emas!";
                return RedirectToAction("Arxivlanganlar");
            }

            foydalanuvchi.ArxivlanganSana = null;
            foydalanuvchi.Faolmi = true;
            foydalanuvchi.YangilanganVaqt = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["xabar"] = $"✅ {foydalanuvchi.Familiya} {foydalanuvchi.Ism} arxivdan ochildi!";
            return RedirectToAction("Arxivlanganlar");
        }

        // 📋 Arxivlangan foydalanuvchilar ro'yxati
        public IActionResult Arxivlanganlar(string? q)
        {
            var query = _context.Foydalanuvchilar
                .Where(f => f.ArxivlanganSana.HasValue)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(f =>
                    (f.Ism != null && f.Ism.ToLower().Contains(term)) ||
                    (f.Familiya != null && f.Familiya.ToLower().Contains(term)) ||
                    (f.Login != null && f.Login.ToLower().Contains(term)) ||
                    (f.TelefonRaqam != null && f.TelefonRaqam.ToLower().Contains(term)));
            }

            ViewBag.QidirilganIsm = q;

            var arxivlanganlar = query
                .OrderByDescending(f => f.ArxivlanganSana)
                .ThenBy(f => f.Familiya)
                .ThenBy(f => f.Ism)
                .ToList();

            return View(arxivlanganlar);
        }

        // 📚 Kurslar ro‘yxati
        public IActionResult Kurslar()
        {
            var kurslar = _context.Kurslar.ToList();
            return View(kurslar);
        }

        // 📘 Kurs qo‘shish
        [HttpGet]
        public IActionResult KursQoshish()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult KursQoshish(Kurs model)
        {
            if (!ModelState.IsValid)
                return View(model);

            model.YaratilganVaqt = DateTime.Now;
            _context.Kurslar.Add(model);
            _context.SaveChanges();

            return RedirectToAction("Kurslar");
        }

        // ✏️ Kurs tahrirlash
        [HttpGet]
        public IActionResult KursTahrirlash(int id)
        {
            var kurs = _context.Kurslar.Find(id);
            if (kurs == null) return NotFound();
            return View(kurs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult KursTahrirlash(Kurs model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var kurs = _context.Kurslar.Find(model.Id);
            if (kurs == null) return NotFound();

            kurs.Nomi = model.Nomi;
            kurs.Tavsif = model.Tavsif;
            kurs.Darajasi = model.Darajasi;
            kurs.Narxi = model.Narxi;
            kurs.YangilanganVaqt = DateTime.Now;

            _context.SaveChanges();
            return RedirectToAction("Kurslar");
        }

        // 🗑 Kurs o‘chirish
        [HttpGet]
        public IActionResult KursOchirish(int id)
        {
            var kurs = _context.Kurslar.Find(id);
            if (kurs == null) return NotFound();
            return View(kurs);
        }

        [HttpPost, ActionName("KursOchirish")]
        [ValidateAntiForgeryToken]
        public IActionResult KursOchirishTasdiqlash(int id)
        {
            var kurs = _context.Kurslar.Find(id);
            if (kurs == null) return NotFound();

            _context.Kurslar.Remove(kurs);
            _context.SaveChanges();

            return RedirectToAction("Kurslar");
        }
        [HttpGet]
        public async Task<IActionResult> BotUlash(string search)
        {
            var query = _context.Foydalanuvchilar.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(f =>
                    (f.Ism ?? string.Empty).ToLower().Contains(term) ||
                    (f.Familiya ?? string.Empty).ToLower().Contains(term) ||
                    (f.OtasiningIsmi ?? string.Empty).ToLower().Contains(term));
            }

            // RoleHelper.IsStudent EF tomonidan translate bo'lmagani uchun, studentlarni xotirada filtrlaymiz
            var talabalar = (await query.ToListAsync())
                .Where(f => RoleHelper.IsStudent(f.Rol))
                .ToList();

            return View(talabalar);
        }

        // GET: ChatId kiritish formasi
        [HttpGet]
        public async Task<IActionResult> ChatIdKirit(int id)
        {
            var foydalanuvchilar = await _context.Foydalanuvchilar.ToListAsync();
            var talaba = foydalanuvchilar
                .FirstOrDefault(f => f.Id == id && RoleHelper.IsStudent(f.Rol));

            if (talaba == null)
            {
                TempData["ErrorMessage"] = "Talaba topilmadi!";
                return RedirectToAction("BotUlash");
            }

            return View(talaba);
        }

        // POST: ChatId saqlash
        [HttpPost]
        public async Task<IActionResult> ChatIdKirit(int id, string chatId)
        {
            var foydalanuvchilar = await _context.Foydalanuvchilar.ToListAsync();
            var talaba = foydalanuvchilar
                .FirstOrDefault(f => f.Id == id && RoleHelper.IsStudent(f.Rol));

            if (talaba == null)
            {
                TempData["ErrorMessage"] = "Talaba topilmadi!";
                return RedirectToAction("BotUlash");
            }

            talaba.ChatId = chatId;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Chat ID muvaffaqiyatli saqlandi!";
            return RedirectToAction("BotUlash");
        }

    }
    
}
