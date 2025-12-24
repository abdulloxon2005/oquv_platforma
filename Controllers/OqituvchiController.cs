using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using talim_platforma.Data;
using talim_platforma.Models;
using talim_platforma.Models.ViewModels;
using talim_platforma.Services;

namespace talim_platforma.Controllers
{
    [Authorize(Roles = "teacher,admin")]
    [AutoValidateAntiforgeryToken]
    public class OqituvchiController : Controller
    {
        private static readonly string[] YaroqliHolatlar = { "Keldi", "Kech keldi", "Kelmadi" };

        private readonly ApplicationDbContext _context;
        private readonly IStudentStatusService _studentStatusService;
        private readonly TelegramBotService _telegramBot;
        private readonly PasswordHasher<Foydalanuvchi> _passwordHasher;

        public OqituvchiController(ApplicationDbContext context, IStudentStatusService studentStatusService, TelegramBotService telegramBot)
        {
            _context = context;
            _studentStatusService = studentStatusService;
            _telegramBot = telegramBot;
            _passwordHasher = new PasswordHasher<Foydalanuvchi>();
        }

        public async Task<IActionResult> Index()
        {
            var teacher = GetCurrentTeacher();
            if (teacher == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.Oqituvchi = teacher;

            var guruhlar = await _context.Guruhlar
                .Include(g => g.Kurs)
                .Include(g => g.TalabaGuruhlar!)
                    .ThenInclude(tg => tg.Talaba)
                .Where(g => g.OqituvchiId == teacher.Id)
                .OrderBy(g => g.DarsVaqti)
                .ToListAsync();

            var bugun = DateTime.Today;
            var bugungiGuruhIds = guruhlar
                .Where(g => GroupHasLessonOn(g, bugun))
                .Select(g => g.Id)
                .ToList();

            var bugungiDavomatlar = await _context.Davomatlar
                .Where(d => d.OqituvchiId == teacher.Id && d.Sana.Date == bugun)
                .Select(d => d.GuruhId)
                .Distinct()
                .ToListAsync();

            var barchaTalabalar = guruhlar
                .SelectMany(g => g.TalabaGuruhlar ?? new List<TalabaGuruh>())
                .Select(tg => tg.TalabaId)
                .Distinct()
                .Count();

            var oxirgiDavomat = await _context.Davomatlar
                .Include(d => d.Talaba)
                .Include(d => d.Guruh)
                .Where(d => d.OqituvchiId == teacher.Id)
                .OrderByDescending(d => d.YangilanganVaqt)
                .Take(6)
                .ToListAsync();

            var model = new TeacherDashboardViewModel
            {
                GuruhlarSoni = guruhlar.Count,
                TalabalarSoni = barchaTalabalar,
                BugungiDarslar = bugungiGuruhIds.Count,
                BugungiDavomatOlindi = bugungiGuruhIds.Count(id => bugungiDavomatlar.Contains(id)),
                Guruhlar = guruhlar.Select(g => new TeacherGroupCard
                {
                    Id = g.Id,
                    Nomi = g.Nomi,
                    KursNomi = g.Kurs?.Nomi ?? "Kurs",
                    DarsVaqti = g.DarsVaqti.ToString("HH:mm"),
                    DarsKunlari = g.DarsKunlari ?? string.Empty,
                    TalabalarSoni = g.TalabaGuruhlar?.Count ?? 0,
                    Holati = g.Holati ?? "Faol"
                }).ToList(),
                OxirgiDavomatlar = oxirgiDavomat.Select(d => new TeacherAttendanceSnapshot
                {
                    Sana = d.Sana,
                    GuruhNomi = d.Guruh?.Nomi ?? "-",
                    TalabaFIO = $"{d.Talaba?.Ism} {d.Talaba?.Familiya}".Trim(),
                    Holati = d.Holati
                }).ToList()
            };

            return View(model);
        }

        public IActionResult Groups(int? id, int? month, int? year, string? section)
        {
            var teacher = GetCurrentTeacher();
            if (teacher == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.Oqituvchi = teacher;

            month ??= DateTime.Now.Month;
            year ??= DateTime.Now.Year;
            var guruhlar = _context.Guruhlar
                .Include(g => g.Kurs)
                .Include(g => g.TalabaGuruhlar)
                    .ThenInclude(tg => tg.Talaba)
                .Where(g => g.OqituvchiId == teacher.Id)
                .OrderBy(g => g.DarsVaqti)
                .ToList();

            Guruh? tanlanganGuruh = null;

            if (id.HasValue)
            {
                tanlanganGuruh = guruhlar.FirstOrDefault(g => g.Id == id.Value);
            }
            else if (guruhlar.Any())
            {
                tanlanganGuruh = guruhlar.First();
            }

            var talabalar = new List<Foydalanuvchi>();
            var davomatlar = new List<Davomat>();
            var darsKunlari = new List<DateTime>();

            if (tanlanganGuruh != null)
            {
                talabalar = _context.TalabaGuruhlar
                    .Include(tg => tg.Talaba)
                    .Where(tg => tg.GuruhId == tanlanganGuruh.Id)
                    .Select(tg => tg.Talaba)
                    .Where(t => t != null)
                    .Cast<Foydalanuvchi>()
                    .ToList();

                var startDate = new DateTime(year.Value, month.Value, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                davomatlar = _context.Davomatlar
                    .Include(d => d.Talaba)
                    .Where(d => d.GuruhId == tanlanganGuruh.Id &&
                                d.Sana >= startDate &&
                                d.Sana <= endDate)
                    .OrderBy(d => d.Sana)
                    .ToList();

                var kunlar = (tanlanganGuruh.DarsKunlari ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(k => k.Trim())
                    .ToHashSet();

                var current = startDate;
                while (current <= endDate)
                {
                    var shortName = GetDayShortName(current.DayOfWeek);
                    if (kunlar.Contains(shortName))
                    {
                        darsKunlari.Add(current);
                    }

                    current = current.AddDays(1);
                }
            }

            ViewBag.Guruhlar = guruhlar;
            ViewBag.TanlanganGuruh = tanlanganGuruh;
            ViewBag.Talabalar = talabalar;
            ViewBag.Davomatlar = davomatlar;
            ViewBag.DarsKunlari = darsKunlari;
            ViewBag.Month = month.Value;
            ViewBag.Year = year.Value;
            ViewBag.OpenAttendanceModal = string.Equals(section, "MarkAttendance", StringComparison.OrdinalIgnoreCase);

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> LoadAttendance(int guruhId, DateTime sana)
        {
            var teacher = GetCurrentTeacher();
            if (teacher == null)
            {
                return Unauthorized();
            }

            var guruh = await _context.Guruhlar
                .Include(g => g.TalabaGuruhlar!)
                    .ThenInclude(tg => tg.Talaba)
                .FirstOrDefaultAsync(g => g.Id == guruhId && g.OqituvchiId == teacher.Id);

            if (guruh == null)
            {
                return NotFound(new { message = "Guruh topilmadi." });
            }

            var requestedDate = sana.Date;

            var davomatlar = await _context.Davomatlar
                .Where(d => d.GuruhId == guruhId && d.Sana.Date == requestedDate)
                .ToListAsync();

            var response = guruh.TalabaGuruhlar!
                .Where(tg => tg.Talaba != null)
                .Select(tg =>
                {
                    var record = davomatlar.FirstOrDefault(d => d.TalabaId == tg.TalabaId);
                    return new
                    {
                        talabaId = tg.TalabaId,
                        fio = $"{tg.Talaba!.Ism} {tg.Talaba.Familiya}".Trim(),
                        holat = record?.Holati ?? string.Empty,
                        yangilangan = record?.YangilanganVaqt
                    };
                })
                .OrderBy(t => t.fio)
                .ToList();

            return Json(new { success = true, students = response });
        }

        [HttpPost]
        public async Task<IActionResult> SaveAttendance([FromBody] AttendanceSaveRequest request)
        {
            if (request == null || request.GuruhId <= 0)
            {
                return BadRequest(new { message = "Noto‘g‘ri ma'lumot yuborildi." });
            }

            var teacher = GetCurrentTeacher();
            if (teacher == null)
            {
                return Unauthorized();
            }

            var guruh = await _context.Guruhlar
                .Include(g => g.TalabaGuruhlar)
                .FirstOrDefaultAsync(g => g.Id == request.GuruhId && g.OqituvchiId == teacher.Id);

            if (guruh == null)
            {
                return NotFound(new { message = "Guruh topilmadi." });
            }

            var sana = request.Sana == default ? DateTime.Today : request.Sana.Date;

            var talabaIds = guruh.TalabaGuruhlar?
                .Select(tg => tg.TalabaId)
                .ToHashSet() ?? new HashSet<int>();

            var existingRecords = await _context.Davomatlar
                .Where(d => d.GuruhId == request.GuruhId && d.Sana.Date == sana)
                .ToListAsync();

            var now = DateTime.Now;
            var changedRecords = new List<Davomat>();
            var filteredStudents = request.Talabalar?
                .Where(t => talabaIds.Contains(t.TalabaId) && !string.IsNullOrWhiteSpace(t.Holati))
                .ToList() ?? new List<AttendanceStudentRequest>();

            if (!filteredStudents.Any())
            {
                return BadRequest(new { message = "Hech bir talaba uchun holat tanlanmadi." });
            }

            var dars = await EnsureDailyLessonAsync(guruh.Id, teacher.Id, sana);

            foreach (var talaba in filteredStudents)
            {
                if (!YaroqliHolatlar.Contains(talaba.Holati!))
                {
                    continue;
                }

                var record = existingRecords.FirstOrDefault(d => d.TalabaId == talaba.TalabaId);
                if (record == null)
                {
                    var yangi = new Davomat
                    {
                        GuruhId = guruh.Id,
                        TalabaId = talaba.TalabaId,
                        DarsId = dars.Id,
                        OqituvchiId = teacher.Id,
                        Sana = sana,
                        Holati = talaba.Holati!,
                        Izoh = talaba.Izoh,
                        YaratilganVaqt = now,
                        YangilanganVaqt = now
                    };
                    _context.Davomatlar.Add(yangi);
                    changedRecords.Add(yangi);
                }
                else if (record.Holati != talaba.Holati || record.Izoh != talaba.Izoh)
                {
                    record.Holati = talaba.Holati!;
                    record.Izoh = talaba.Izoh;
                    record.YangilanganVaqt = now;
                    changedRecords.Add(record);
                }
            }

            if (!changedRecords.Any())
            {
                return Json(new { success = true, message = "O'zgarishlar topilmadi." });
            }

            await _context.SaveChangesAsync();

            foreach (var record in changedRecords)
            {
                await _studentStatusService.RefreshStudentStatusAsync(record.TalabaId, record.GuruhId);
                await SendAttendanceNotificationAsync(record, guruh, sana);
            }

            return Json(new { success = true, updated = changedRecords.Count });
        }

        [HttpGet]
        public IActionResult Profile()
        {
            var teacher = GetCurrentTeacher();
            if (teacher == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.Oqituvchi = teacher;
            ViewBag.SuccessMessage = TempData["SuccessMessage"] as string;
            ViewBag.ErrorMessage = TempData["ErrorMessage"] as string;

            return View(teacher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(
            string eskiParol,
            string yangiParol,
            string yangiParolTasdiq,
            string ism,
            string familiya,
            string otasiningIsmi,
            string telefonRaqam)
        {
            var teacher = GetCurrentTeacher();
            if (teacher == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var foydalanuvchi = await _context.Foydalanuvchilar.FindAsync(teacher.Id);
            if (foydalanuvchi == null)
            {
                TempData["ErrorMessage"] = "Foydalanuvchi topilmadi.";
                return RedirectToAction("Profile");
            }

            bool maLumotlarOzgardi = false;
            bool parolOzgardi = false;

            // Parol o'zgartirish
            if (!string.IsNullOrWhiteSpace(eskiParol) || 
                !string.IsNullOrWhiteSpace(yangiParol) || 
                !string.IsNullOrWhiteSpace(yangiParolTasdiq))
            {
                if (string.IsNullOrWhiteSpace(eskiParol))
                {
                    TempData["ErrorMessage"] = "Eski parol kiritilishi shart.";
                    ViewBag.Oqituvchi = foydalanuvchi;
                    return View(foydalanuvchi);
                }

                if (string.IsNullOrWhiteSpace(yangiParol) || string.IsNullOrWhiteSpace(yangiParolTasdiq))
                {
                    TempData["ErrorMessage"] = "Yangi parol va tasdiq parolini kiriting.";
                    ViewBag.Oqituvchi = foydalanuvchi;
                    return View(foydalanuvchi);
                }

                if (yangiParol != yangiParolTasdiq)
                {
                    TempData["ErrorMessage"] = "Yangi parol va tasdiq paroli mos kelmaydi.";
                    ViewBag.Oqituvchi = foydalanuvchi;
                    return View(foydalanuvchi);
                }

                if (yangiParol.Length < 6)
                {
                    TempData["ErrorMessage"] = "Yangi parol kamida 6 ta belgidan iborat bo'lishi kerak.";
                    ViewBag.Oqituvchi = foydalanuvchi;
                    return View(foydalanuvchi);
                }

                // Eski parolni tekshirish
                var result = _passwordHasher.VerifyHashedPassword(foydalanuvchi, foydalanuvchi.Parol, eskiParol);
                if (result == PasswordVerificationResult.Failed)
                {
                    TempData["ErrorMessage"] = "Eski parol noto'g'ri.";
                    ViewBag.Oqituvchi = foydalanuvchi;
                    return View(foydalanuvchi);
                }

                // Yangi parolni hash qilish
                foydalanuvchi.Parol = _passwordHasher.HashPassword(foydalanuvchi, yangiParol);
                parolOzgardi = true;
                maLumotlarOzgardi = true;
            }

            // Shaxsiy ma'lumotlarni yangilash
            if (!string.IsNullOrWhiteSpace(ism) && foydalanuvchi.Ism != ism.Trim())
            {
                foydalanuvchi.Ism = ism.Trim();
                maLumotlarOzgardi = true;
            }

            if (!string.IsNullOrWhiteSpace(familiya) && foydalanuvchi.Familiya != familiya.Trim())
            {
                foydalanuvchi.Familiya = familiya.Trim();
                maLumotlarOzgardi = true;
            }

            if (foydalanuvchi.OtasiningIsmi != otasiningIsmi?.Trim())
            {
                foydalanuvchi.OtasiningIsmi = otasiningIsmi?.Trim() ?? string.Empty;
                maLumotlarOzgardi = true;
            }

            if (!string.IsNullOrWhiteSpace(telefonRaqam) && foydalanuvchi.TelefonRaqam != telefonRaqam.Trim())
            {
                foydalanuvchi.TelefonRaqam = telefonRaqam.Trim();
                maLumotlarOzgardi = true;
            }

            if (maLumotlarOzgardi)
            {
                foydalanuvchi.YangilanganVaqt = DateTime.Now;
                await _context.SaveChangesAsync();

                if (parolOzgardi)
                {
                    TempData["SuccessMessage"] = "Parol va shaxsiy ma'lumotlar muvaffaqiyatli yangilandi.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Shaxsiy ma'lumotlar muvaffaqiyatli yangilandi.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Hech qanday o'zgarish kiritilmadi.";
            }

            ViewBag.Oqituvchi = foydalanuvchi;
            return RedirectToAction("Profile");
        }

        private Foydalanuvchi? GetCurrentTeacher()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? HttpContext.Session.GetString("FoydalanuvchiId");

            if (!int.TryParse(idStr, out var userId))
            {
                return null;
            }

            var user = _context.Foydalanuvchilar.FirstOrDefault(f => f.Id == userId);
            if (user == null)
            {
                return null;
            }

            var role = (user.Rol ?? string.Empty).Trim().ToLower();
            if (role is "teacher" or "oqituvchi" or "admin")
            {
                return user;
            }

            return null;
        }

        private static string GetDayShortName(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => "Du",
                DayOfWeek.Tuesday => "Se",
                DayOfWeek.Wednesday => "Cho",
                DayOfWeek.Thursday => "Pay",
                DayOfWeek.Friday => "Ju",
                DayOfWeek.Saturday => "Shan",
                DayOfWeek.Sunday => "Yak",
                _ => string.Empty
            };
        }

        private static bool GroupHasLessonOn(Guruh guruh, DateTime sana)
        {
            if (string.IsNullOrWhiteSpace(guruh.DarsKunlari))
            {
                return false;
            }

            var shortName = GetDayShortName(sana.DayOfWeek);
            return (guruh.DarsKunlari ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Any(k => k.Trim().Equals(shortName, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<Dars> EnsureDailyLessonAsync(int guruhId, int oqituvchiId, DateTime sana)
        {
            var dars = await _context.Darslar
                .FirstOrDefaultAsync(d => d.GuruhId == guruhId && d.Sana.Date == sana);

            if (dars != null)
            {
                return dars;
            }

            dars = new Dars
            {
                GuruhId = guruhId,
                OqituvchiId = oqituvchiId,
                Sana = sana,
                Mavzu = $"{sana:dd.MM.yyyy} darsi",
                DarsTuri = "Asosiy",
                Davomiylik = "90 daqiqa",
                YaratilganVaqt = DateTime.Now,
                YangilanganVaqt = DateTime.Now
            };

            _context.Darslar.Add(dars);
            await _context.SaveChangesAsync();

            return dars;
        }

        private async Task SendAttendanceNotificationAsync(Davomat record, Guruh guruh, DateTime sana)
        {
            var talaba = await _context.Foydalanuvchilar.FirstOrDefaultAsync(t => t.Id == record.TalabaId);
            if (talaba == null || string.IsNullOrWhiteSpace(talaba.ChatId))
            {
                return;
            }

            if (!long.TryParse(talaba.ChatId, out var chatId))
            {
                return;
            }

            var holatMatni = record.Holati switch
            {
                "Keldi" => "darsga o'z vaqtida keldi ✅",
                "Kech keldi" => "darsga kechikib keldi ⏰",
                "Kelmadi" => "darsga kelmadi ❌",
                _ => $"holati: {record.Holati}"
            };

            var xabar =
                $"📚 Farzandingiz: {talaba.Ism} {talaba.Familiya}\n" +
                $"👨‍🏫 Guruh: {guruh.Nomi}\n" +
                $"📅 Sana: {sana:dd.MM.yyyy}\n" +
                $"🕒 Yozilgan vaqt: {DateTime.Now:HH:mm}\n" +
                $"ℹ️ Natija: {holatMatni}";

            await _telegramBot.SendMessageAsync(chatId, xabar);
        }

        public class AttendanceSaveRequest
        {
            public int GuruhId { get; set; }
            public DateTime Sana { get; set; }
            public List<AttendanceStudentRequest> Talabalar { get; set; } = new();
        }

        public class AttendanceStudentRequest
        {
            public int TalabaId { get; set; }
            public string? Holati { get; set; }
            public string? Izoh { get; set; }
        }
    }
}

