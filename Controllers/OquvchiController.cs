using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using talim_platforma.Data;
using talim_platforma.Models;
using talim_platforma.Models.ViewModels;

namespace talim_platforma.Controllers;

[Authorize(Roles = "student")]
public class OquvchiController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OquvchiController> _logger;
    private readonly PasswordHasher<Foydalanuvchi> _passwordHasher;

    public OquvchiController(ApplicationDbContext context, ILogger<OquvchiController> logger)
    {
        _context = context;
        _logger = logger;
        _passwordHasher = new PasswordHasher<Foydalanuvchi>();
    }

    public async Task<IActionResult> DashboardTalaba()
    {
        var ctx = await GetStudentContextAsync();
        if (ctx == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        var (talabaId, talaba) = ctx.Value;

        var guruhlar = await _context.TalabaGuruhlar
            .Include(tg => tg.Guruh)
                .ThenInclude(g => g.Kurs)
            .Include(tg => tg.Guruh)
                .ThenInclude(g => g.TalabaGuruhlar)
            .Where(tg => tg.TalabaId == talabaId)
            .ToListAsync();

        var guruhIds = guruhlar.Where(tg => tg.Guruh != null).Select(tg => tg.GuruhId).ToList();
        var kursIds = guruhlar.Where(tg => tg.Guruh?.KursId != null).Select(tg => tg.Guruh!.KursId).Distinct().ToList();
        var kurslar = await _context.TalabaKurslar
            .Include(tk => tk.Kurs)
            .Where(tk => tk.TalabaId == talabaId)
            .ToListAsync();

        var tolovlar = await _context.Tolovlar
            .Include(t => t.Kurs)
            .Where(t => t.TalabaId == talabaId)
            .OrderByDescending(t => t.Sana)
            .Take(6)
            .ToListAsync();

        var barchaTolovlar = await _context.Tolovlar
            .Where(t => t.TalabaId == talabaId)
            .OrderByDescending(t => t.Sana)
            .ToListAsync();

        var latestTolovByKurs = barchaTolovlar
            .GroupBy(t => t.KursId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.Sana).FirstOrDefault());

        var davomatlar = await _context.Davomatlar
            .Include(d => d.Guruh)
            .Where(d => d.TalabaId == talabaId)
            .OrderByDescending(d => d.Sana)
            .Take(8)
            .ToListAsync();

        var natijalar = await _context.ImtihonNatijalar
            .Include(n => n.Imtihon)
                .ThenInclude(i => i.Guruh)
                    .ThenInclude(g => g.Kurs)
            .Where(n => n.TalabaId == talabaId)
            .OrderByDescending(n => n.Sana)
            .Take(6)
            .ToListAsync();

        // Baholarni olish
        var baholar = await _context.Baholar
            .Include(b => b.Dars)
                .ThenInclude(d => d.Guruh)
                    .ThenInclude(g => g.Kurs)
            .Where(b => b.TalabaId == talabaId)
            .OrderByDescending(b => b.YaratilganVaqt)
            .Take(10)
            .ToListAsync();

        var attendanceLookup = await _context.Davomatlar
            .Where(d => d.TalabaId == talabaId && guruhIds.Contains(d.GuruhId))
            .Select(d => new { d.GuruhId, Sana = d.Sana.Date, d.Holati })
            .ToListAsync();
        var attendanceMap = attendanceLookup
            .GroupBy(x => (x.GuruhId, x.Sana))
            .ToDictionary(g => g.Key, g => g.First().Holati ?? string.Empty);
        var allowedAttendance = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "keldi", "kech keldi" };

        var activeOnlineExams = await _context.Imtihonlar
            .Include(i => i.Guruh)
                .ThenInclude(g => g.Kurs)
            .Where(i => i.Faolmi
                        && i.ImtihonFormati == "Online"
                        && guruhIds.Contains(i.GuruhId))
            .OrderBy(i => i.Sana)
            .ThenBy(i => i.BoshlanishVaqti)
            .Take(6)
            .ToListAsync();

        // Tangacha miqdorini olish
        var tangacha = talaba?.Tangacha ?? 0m;

        var model = new StudentDashboardViewModel
        {
            FullName = ViewBag.FullName as string ?? string.Empty,
            FaolGuruhlar = guruhIds.Count,
            FaolKurslar = kursIds.Count,
            JamiTolov = barchaTolovlar.Sum(t => t.Miqdor),
            Qarzdorlik = barchaTolovlar.Sum(t => t.Qarzdorlik),
            Haqdorlik = barchaTolovlar.Sum(t => t.Haqdorlik),
            OxirgiFoiz = natijalar.FirstOrDefault()?.FoizNatija,
            Tangacha = tangacha,
            Tolovlar = tolovlar.Select(t => new StudentPaymentItem
            {
                Sana = t.Sana,
                Kurs = t.Kurs?.Nomi ?? "Kurs",
                Miqdor = t.Miqdor,
                TolovUsuli = t.TolovUsuli,
                Qarzdorlik = t.Qarzdorlik,
                Haqdorlik = t.Haqdorlik,
                ChekRaqami = t.ChekRaqami ?? string.Empty,
                Holat = t.Holat ?? string.Empty
            }).ToList(),
            Davomatlar = davomatlar.Select(d => new StudentAttendanceItem
            {
                Sana = d.Sana,
                Holati = d.Holati,
                Guruh = d.Guruh?.Nomi ?? "-"
            }).ToList(),
            Natijalar = natijalar.Select(n => new StudentExamResultItem
            {
                NatijaId = n.Id,
                ImtihonNomi = n.Imtihon?.Nomi ?? "Imtihon",
                Guruh = n.Imtihon?.Guruh?.Nomi ?? "-",
                Sana = n.Sana,
                Foiz = n.FoizNatija,
                Otdimi = n.Otdimi
            }).ToList(),
            Guruhlar = guruhlar
                .Where(g => g.Guruh != null)
                .Select(g => new StudentGroupCard
                {
                    Id = g.Guruh!.Id,
                    Nomi = g.Guruh.Nomi,
                    Kurs = g.Guruh.Kurs?.Nomi ?? "Kurs",
                    DarsVaqti = g.Guruh.DarsVaqti.ToString("HH:mm"),
                    DarsKunlari = g.Guruh.DarsKunlari ?? "",
                    TalabalarSoni = g.Guruh.TalabaGuruhlar?.Count ?? 0
                }).ToList(),
            OnlineImtihonlar = activeOnlineExams.Select(i =>
            {
                var key = (i.GuruhId, i.Sana.Date);
                var holat = attendanceMap.TryGetValue(key, out var found) ? found : string.Empty;
                var eligible = allowedAttendance.Contains(holat ?? string.Empty);
                var message = string.IsNullOrWhiteSpace(holat)
                    ? "Davomat hali belgilanmagan."
                    : eligible ? "Imtihon uchun ruxsat berildi." : $"Holat: {holat}";

                return new StudentExamCard
                {
                    Id = i.Id,
                    Nomi = i.Nomi,
                    Guruh = i.Guruh?.Nomi ?? "-",
                    Kurs = i.Guruh?.Kurs?.Nomi ?? "-",
                    Sana = i.Sana,
                    MuddatDaqiqada = i.MuddatDaqiqada,
                    IsEligible = eligible,
                    EligibilityMessage = message
                };
            }).ToList(),
            Kurslar = kurslar
                .Where(k => k.Kurs != null)
                .GroupBy(k => k.KursId)
                .Select(g => new StudentCourseItem
                {
                    Nomi = g.First().Kurs!.Nomi,
                    Holati = ResolveTolovStatus(latestTolovByKurs.TryGetValue(g.Key, out var lt) ? lt : null, g.First().Holati),
                    Narx = g.First().Kurs!.Narxi
                }).ToList(),
            Baholar = baholar.Select(b => new StudentBahoItem
            {
                Sana = b.Dars?.Sana ?? b.YaratilganVaqt,
                Baho = b.Ball,
                KursNomi = b.Dars?.Guruh?.Kurs?.Nomi ?? "Kurs",
                GuruhNomi = b.Dars?.Guruh?.Nomi ?? "Guruh",
                Mavzu = b.Dars?.Mavzu ?? "Dars"
            }).ToList()
        };

        return View(model);
    }

    public async Task<IActionResult> PaymentHistory()
    {
        var ctx = await GetStudentContextAsync();
        if (ctx == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        var talabaId = ctx.Value.talabaId;

        var tolovlar = await _context.Tolovlar
            .Include(t => t.Kurs)
            .Where(t => t.TalabaId == talabaId)
            .OrderByDescending(t => t.Sana)
            .ToListAsync();

        ViewBag.Qarzdorlik = tolovlar.Sum(t => t.Qarzdorlik);
        ViewBag.Haqdorlik = tolovlar.Sum(t => t.Haqdorlik);

        return View(tolovlar);
    }

    public async Task<IActionResult> Profil()
    {
        var ctx = await GetStudentContextAsync();
        if (ctx == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        var talaba = ctx.Value.talaba;
        if (talaba == null)
        {
            TempData["ErrorMessage"] = "Profil ma'lumotlari topilmadi.";
            return RedirectToAction("DashboardTalaba");
        }

        var vm = new StudentProfileUpdateViewModel
        {
            User = talaba,
            TelefonRaqam = talaba.TelefonRaqam ?? string.Empty
        };

        ViewData["Title"] = "Profil";
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profil(StudentProfileUpdateViewModel model)
    {
        var ctx = await GetStudentContextAsync();
        if (ctx == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        var talaba = ctx.Value.talaba;
        if (talaba == null)
        {
            TempData["ErrorMessage"] = "Profil ma'lumotlari topilmadi.";
            return RedirectToAction("DashboardTalaba");
        }

        if (!ModelState.IsValid)
        {
            model.User = talaba;
            ViewData["Title"] = "Profil";
            return View(model);
        }

        talaba.TelefonRaqam = model.TelefonRaqam.Trim();

        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            if (string.IsNullOrWhiteSpace(model.OldPassword))
            {
                ModelState.AddModelError(nameof(model.OldPassword), "Avvalgi parolni kiriting.");
                model.User = talaba;
                ViewData["Title"] = "Profil";
                return View(model);
            }

            var verify = _passwordHasher.VerifyHashedPassword(talaba, talaba.Parol, model.OldPassword.Trim());
            if (verify == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(nameof(model.OldPassword), "Avvalgi parol noto'g'ri.");
                model.User = talaba;
                ViewData["Title"] = "Profil";
                return View(model);
            }

            talaba.Parol = _passwordHasher.HashPassword(talaba, model.NewPassword.Trim());
        }

        talaba.YangilanganVaqt = DateTime.Now;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Profil yangilandi.";
        return RedirectToAction("Profil");
    }

    private async Task<(int talabaId, Foydalanuvchi? talaba)?> GetStudentContextAsync()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                       ?? HttpContext.Session.GetString("FoydalanuvchiId");

        if (!int.TryParse(userIdStr, out var talabaId))
        {
            return null;
        }

        var talaba = await _context.Foydalanuvchilar.FirstOrDefaultAsync(f => f.Id == talabaId);
        
        // Arxivlangan foydalanuvchini tekshirish
        if (talaba != null && talaba.Arxivlanganmi && talaba.ArxivlanganSana.HasValue)
        {
            var kunlarOtdi = (DateTime.Now - talaba.ArxivlanganSana.Value).Days;
            
            // 1 oydan (30 kun) keyin kirishni bloklash
            if (kunlarOtdi >= 30)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                HttpContext.Session.Clear();
                TempData["ErrorMessage"] = "Sizning hisobingiz arxivlangan va 1 oy o'tgan. Kirish imkoni yo'q.";
                return null;
            }
            
            // 1 oy ichida bo'lsa, faqat o'z ma'lumotlarini ko'ra oladi
            ViewBag.Arxivlanganmi = true;
            ViewBag.ArxivlanganSana = talaba.ArxivlanganSana.Value;
            ViewBag.KunlarOtdi = kunlarOtdi;
        }
        
        var fullName = $"{talaba?.Ism} {talaba?.Familiya}".Trim();
        if (string.IsNullOrWhiteSpace(fullName))
        {
            fullName = User.Claims.FirstOrDefault(c => c.Type == "FullName")?.Value
                       ?? User.Identity?.Name
                       ?? "Talaba";
        }

        ViewBag.FullName = fullName;
        ViewBag.Talaba = talaba;

        return (talabaId, talaba);
    }

    private static string ResolveTolovStatus(Tolov? latest, string fallbackStatus)
    {
        if (latest != null)
        {
            if (latest.Qarzdorlik > 0) return "Qarzdor";
            if (latest.Haqdorlik > 0) return "Haqdor";
            return "To‘liq";
        }

        return string.IsNullOrWhiteSpace(fallbackStatus) ? "Faol" : fallbackStatus;
    }
}

