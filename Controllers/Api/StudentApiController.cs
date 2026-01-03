using System.Security.Claims;
using System.Text.Json;
using System.IO;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using iTextSharp.text;
using iTextSharp.text.pdf;
using talim_platforma.Data;
using talim_platforma.Models;
using talim_platforma.Models.ViewModels;

namespace talim_platforma.Controllers.Api;

[ApiController]
[Route("api/student")]
public class StudentApiController : ControllerBase
{
    private static readonly HashSet<string> AllowedAttendanceStatuses = new(StringComparer.OrdinalIgnoreCase) { "keldi", "kech keldi" };

    private readonly ApplicationDbContext _context;
    private readonly ILogger<StudentApiController> _logger;
    private readonly PasswordHasher<Foydalanuvchi> _passwordHasher;

    public StudentApiController(ApplicationDbContext context, ILogger<StudentApiController> logger)
    {
        _context = context;
        _logger = logger;
        _passwordHasher = new PasswordHasher<Foydalanuvchi>();
    }

    // 🔑 Login – cookie bilan (mobil ilova cookie-ni saqlaydi)
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Parol))
        {
            return BadRequest(new { message = "Login va parol talab qilinadi." });
        }

        var user = await _context.Foydalanuvchilar.FirstOrDefaultAsync(f => f.Login.ToLower() == request.Login.Trim().ToLower());
        if (user == null)
        {
            return Unauthorized(new { message = "Login yoki parol noto‘g‘ri." });
        }

        // Arxivlangan foydalanuvchini tekshirish
        if (user.Arxivlanganmi && user.ArxivlanganSana.HasValue)
        {
            var kunlarOtdi = (DateTime.Now - user.ArxivlanganSana.Value).Days;
            
            // 1 oydan (30 kun) keyin kirishni bloklash
            if (kunlarOtdi >= 30)
            {
                return Unauthorized(new { message = "Sizning hisobingiz arxivlangan va 1 oy o'tgan. Kirish imkoni yo'q." });
            }
        }

        var verified = _passwordHasher.VerifyHashedPassword(user, user.Parol, request.Parol.Trim());
        if (verified == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "Login yoki parol noto‘g‘ri." });
        }

        var role = NormalizeRole(user.Rol);
        if (!string.Equals(role, "student", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(role, "oquvchi", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, $"{user.Familiya} {user.Ism}".Trim()),
            new Claim(ClaimTypes.Role, role)
        };

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            authProperties);

        HttpContext.Session.SetString("FoydalanuvchiId", user.Id.ToString());
        HttpContext.Session.SetString("Rol", role);

        return Ok(new
        {
            success = true,
            userId = user.Id,
            fullName = $"{user.Ism} {user.Familiya}".Trim(),
            role = role
        });
    }

    // 📊 Dashboard ma'lumotlari
    [HttpGet("dashboard")]
    [Authorize(Roles = "student")]
    public async Task<IActionResult> Dashboard()
    {
        var ctx = await GetStudentContextAsync();
        if (ctx == null) return Unauthorized(new { message = "Talaba topilmadi." });

        var (talabaId, talaba) = ctx.Value;

        var guruhlar = await _context.TalabaGuruhlar
            .Include(tg => tg.Guruh)!.ThenInclude(g => g.Kurs)
            .Include(tg => tg.Guruh)!.ThenInclude(g => g.TalabaGuruhlar)
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
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Sana).FirstOrDefault());

        var davomatlar = await _context.Davomatlar
            .Include(d => d.Guruh)
            .Where(d => d.TalabaId == talabaId)
            .OrderByDescending(d => d.Sana)
            .Take(8)
            .ToListAsync();

        var natijalar = await _context.ImtihonNatijalar
            .Include(n => n.Imtihon)!.ThenInclude(i => i.Guruh)!.ThenInclude(g => g.Kurs)
            .Where(n => n.TalabaId == talabaId)
            .OrderByDescending(n => n.Sana)
            .Take(6)
            .ToListAsync();

        var attendanceLookup = await _context.Davomatlar
            .Where(d => d.TalabaId == talabaId && guruhIds.Contains(d.GuruhId))
            .Select(d => new { d.GuruhId, Sana = d.Sana.Date, d.Holati })
            .ToListAsync();
        var attendanceMap = attendanceLookup
            .GroupBy(x => (x.GuruhId, x.Sana))
            .ToDictionary(g => g.Key, g => g.First().Holati ?? string.Empty);

        var activeOnlineExams = await _context.Imtihonlar
            .Include(i => i.Guruh)!.ThenInclude(g => g.Kurs)
            .Where(i => i.Faolmi && i.ImtihonFormati == "Online" && guruhIds.Contains(i.GuruhId))
            .OrderBy(i => i.Sana).ThenBy(i => i.BoshlanishVaqti)
            .Take(6)
            .ToListAsync();

        var model = new StudentDashboardViewModel
        {
            FullName = $"{talaba?.Ism} {talaba?.Familiya}".Trim(),
            FaolGuruhlar = guruhIds.Count,
            FaolKurslar = kursIds.Count,
            JamiTolov = barchaTolovlar.Sum(t => t.Miqdor),
            Qarzdorlik = barchaTolovlar.Sum(t => t.Qarzdorlik),
            Haqdorlik = barchaTolovlar.Sum(t => t.Haqdorlik),
            OxirgiFoiz = natijalar.FirstOrDefault()?.FoizNatija,
            Tolovlar = tolovlar.Select(t => new StudentPaymentItem
            {
                Sana = t.Sana,
                Kurs = t.Kurs?.Nomi ?? "Kurs",
                Miqdor = t.Miqdor,
                TolovUsuli = t.TolovUsuli ?? string.Empty,
                Qarzdorlik = t.Qarzdorlik,
                Haqdorlik = t.Haqdorlik,
                ChekRaqami = t.ChekRaqami ?? string.Empty,
                Holat = t.Holat ?? string.Empty
            }).ToList(),
            Davomatlar = davomatlar.Select(d => new StudentAttendanceItem
            {
                Sana = d.Sana,
                Holati = d.Holati ?? string.Empty,
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
                var eligible = AllowedAttendanceStatuses.Contains(holat ?? string.Empty);
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
                }).ToList()
        };

        return Ok(model);
    }

    // 💳 To'lovlar tarixi
    [HttpGet("payments")]
    [Authorize(Roles = "student")]
    public async Task<IActionResult> Payments()
    {
        var ctx = await GetStudentContextAsync();
        if (ctx == null) return Unauthorized();

        var talabaId = ctx.Value.talabaId;
        var tolovlar = await _context.Tolovlar
            .Include(t => t.Kurs)
            .Where(t => t.TalabaId == talabaId)
            .OrderByDescending(t => t.Sana)
            .ToListAsync();

        return Ok(new
        {
            qarzdorlik = tolovlar.Sum(t => t.Qarzdorlik),
            haqdorlik = tolovlar.Sum(t => t.Haqdorlik),
            items = tolovlar.Select(t => new
            {
                t.Id,
                t.Sana,
                t.Miqdor,
                Kurs = t.Kurs?.Nomi,
                t.TolovUsuli,
                t.Qarzdorlik,
                t.Haqdorlik,
                t.ChekRaqami,
                t.Holat
            })
        });
    }

    // 📅 Davomat
    [HttpGet("attendance")]
    [Authorize(Roles = "student")]
    public async Task<IActionResult> Attendance()
    {
        var ctx = await GetStudentContextAsync();
        if (ctx == null) return Unauthorized();
        var talabaId = ctx.Value.talabaId;

        var davomatlar = await _context.Davomatlar
            .Include(d => d.Guruh)
            .Where(d => d.TalabaId == talabaId)
            .OrderByDescending(d => d.Sana)
            .Take(50)
            .ToListAsync();

        return Ok(davomatlar.Select(d => new
        {
            d.Id,
            d.Sana,
            d.Holati,
            d.Izoh,
            Guruh = d.Guruh?.Nomi
        }));
    }

    // 📊 Baholar ro'yxati
    [HttpGet("grades")]
    [Authorize(Roles = "student")]
    public async Task<IActionResult> Grades()
    {
        var ctx = await GetStudentContextAsync();
        if (ctx == null) return Unauthorized();
        var talabaId = ctx.Value.talabaId;

        var baholar = await _context.Baholar
            .Include(b => b.Dars)
                .ThenInclude(d => d.Guruh)
                    .ThenInclude(g => g.Kurs)
            .Where(b => b.TalabaId == talabaId)
            .OrderByDescending(b => b.YaratilganVaqt)
            .ToListAsync();

        return Ok(baholar.Select(b => new
        {
            b.Id,
            Sana = b.Dars?.Sana ?? b.YaratilganVaqt,
            Baho = b.Ball,
            KursNomi = b.Dars?.Guruh?.Kurs?.Nomi ?? "Kurs",
            GuruhNomi = b.Dars?.Guruh?.Nomi ?? "Guruh",
            Mavzu = b.Dars?.Mavzu ?? "Dars",
            Izoh = b.Izoh,
            YaratilganVaqt = b.YaratilganVaqt
        }));
    }

    // 📋 Imtihon natijalari ro'yxati
    [HttpGet("results")]
    [Authorize(Roles = "student")]
    public async Task<IActionResult> Results()
    {
        var ctx = await GetStudentContextAsync();
        if (ctx == null) return Unauthorized();
        var talabaId = ctx.Value.talabaId;

        var natijalar = await _context.ImtihonNatijalar
            .Include(n => n.Imtihon)
                .ThenInclude(i => i.Guruh)
                    .ThenInclude(g => g.Kurs)
            .Where(n => n.TalabaId == talabaId)
            .OrderByDescending(n => n.Sana)
            .ToListAsync();

        return Ok(natijalar.Select(n => new
        {
            n.Id,
            ImtihonId = n.ImtihonId,
            ImtihonNomi = n.Imtihon?.Nomi ?? "Imtihon",
            Guruh = n.Imtihon?.Guruh?.Nomi ?? "-",
            Kurs = n.Imtihon?.Guruh?.Kurs?.Nomi ?? "-",
            n.Sana,
            n.UmumiyBall,
            n.MaksimalBall,
            n.FoizNatija,
            n.Otdimi,
            n.BoshlanishVaqti,
            n.TugashVaqti
        }));
    }

    // 🧪 Online imtihonlar ro'yxati
    [HttpGet("exams/online")]
    [Authorize(Roles = "student")]
    public async Task<IActionResult> OnlineExams()
    {
        var ctx = await GetStudentContextAsync();
        if (ctx == null) return Unauthorized();
        var talabaId = ctx.Value.talabaId;

        var guruhIds = await _context.TalabaGuruhlar
            .Where(tg => tg.TalabaId == talabaId)
            .Select(tg => tg.GuruhId)
            .ToListAsync();

        var exams = await _context.Imtihonlar
            .Include(i => i.Guruh)!.ThenInclude(g => g.Kurs)
            .Where(i => i.Faolmi && i.ImtihonFormati == "Online" && guruhIds.Contains(i.GuruhId))
            .OrderBy(i => i.Sana)
            .ThenBy(i => i.BoshlanishVaqti)
            .ToListAsync();

        return Ok(exams.Select(i => new
        {
            i.Id,
            i.Nomi,
            Guruh = i.Guruh?.Nomi,
            Kurs = i.Guruh?.Kurs?.Nomi,
            i.Sana,
            i.BoshlanishVaqti,
            i.TugashVaqti,
            i.MuddatDaqiqada,
            i.MinimalBall
        }));
    }

    // 🧪 Imtihon tafsiloti + savollar
    [HttpGet("exams/{id:int}")]
    [Authorize(Roles = "student")]
    public async Task<IActionResult> OnlineExamDetail(int id)
    {
        var ctx = await GetStudentContextAsync();
        if (ctx == null) return Unauthorized();
        var talabaId = ctx.Value.talabaId;

        var imtihon = await _context.Imtihonlar
            .Include(i => i.Guruh)
            .Include(i => i.Savollar)
            .FirstOrDefaultAsync(i => i.Id == id && i.Faolmi && i.ImtihonFormati == "Online");

        if (imtihon == null) return NotFound(new { message = "Imtihon topilmadi." });

        var talabaGuruh = await _context.TalabaGuruhlar.FirstOrDefaultAsync(tg => tg.TalabaId == talabaId && tg.GuruhId == imtihon.GuruhId);
        if (talabaGuruh == null) return Forbid();

        var (eligible, reason) = await CheckAttendanceAsync(talabaId, imtihon);
        if (!eligible) return BadRequest(new { message = reason });

        var oldNatija = await _context.ImtihonNatijalar.FirstOrDefaultAsync(n => n.ImtihonId == id && n.TalabaId == talabaId);
        if (oldNatija != null)
        {
            return BadRequest(new { message = "Siz bu imtihonni allaqachon topshirgansiz.", natijaId = oldNatija.Id });
        }

        return Ok(new
        {
            imtihon.Id,
            imtihon.Nomi,
            imtihon.Sana,
            imtihon.MuddatDaqiqada,
            Savollar = imtihon.Savollar?.Select(s => new
            {
                s.Id,
                s.SavolMatni,
                VariantA = s.VariantA,
                VariantB = s.VariantB,
                VariantC = s.VariantC,
                VariantD = s.VariantD,
                Ball = s.BallQiymati
            })
        });
    }

    // 📝 Imtihon javoblarini yuborish
    [HttpPost("exams/submit")]
    [Authorize(Roles = "student")]
    public async Task<IActionResult> SubmitExam([FromBody] OnlineImtihonJavobViewModel model)
    {
        var ctx = await GetStudentContextAsync();
        if (ctx == null) return Unauthorized();
        var talabaId = ctx.Value.talabaId;

        var imtihon = await _context.Imtihonlar
            .Include(i => i.Savollar)
            .FirstOrDefaultAsync(i => i.Id == model.ImtihonId);
        if (imtihon == null) return NotFound(new { message = "Imtihon topilmadi." });
        if (!imtihon.Faolmi || !string.Equals(imtihon.ImtihonFormati, "Online", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Bu imtihon online rejimida emas." });
        }

        var talabaGuruh = await _context.TalabaGuruhlar.FirstOrDefaultAsync(tg => tg.TalabaId == talabaId && tg.GuruhId == imtihon.GuruhId);
        if (talabaGuruh == null) return Forbid();

        var (eligible, reason) = await CheckAttendanceAsync(talabaId, imtihon);
        if (!eligible) return BadRequest(new { message = reason });

        var oldNatija = await _context.ImtihonNatijalar.FirstOrDefaultAsync(n => n.ImtihonId == model.ImtihonId && n.TalabaId == talabaId);
        if (oldNatija != null) return BadRequest(new { message = "Siz bu imtihonni allaqachon topshirgansiz.", natijaId = oldNatija.Id });

        model.Javoblar ??= new List<JavobItem>();

        int umumiyBall = 0;
        int maksimalBall = imtihon.Savollar?.Sum(s => s.BallQiymati) ?? 0;
        int togriSanog = 0;
        var javoblar = new List<object>();

        foreach (var javob in model.Javoblar)
        {
            var savol = imtihon.Savollar?.FirstOrDefault(s => s.Id == javob.SavolId);
            if (savol == null) continue;

            bool togri = savol.TogriJavob?.Trim().ToUpper() == javob.TanlanganJavob?.Trim().ToUpper();
            if (togri)
            {
                umumiyBall += savol.BallQiymati;
                togriSanog++;
            }

            javoblar.Add(new
            {
                SavolId = javob.SavolId,
                TanlanganJavob = javob.TanlanganJavob,
                TogriJavob = savol.TogriJavob,
                Togri = togri,
                Ball = togri ? savol.BallQiymati : 0
            });
        }

        decimal foiz = maksimalBall > 0 ? (decimal)umumiyBall * 100 / maksimalBall : 0;
        bool otdimi = foiz >= imtihon.MinimalBall;

        var natija = new ImtihonNatija
        {
            ImtihonId = model.ImtihonId,
            TalabaId = talabaId,
            UmumiyBall = umumiyBall,
            MaksimalBall = maksimalBall,
            FoizNatija = foiz,
            Otdimi = otdimi,
            Sana = DateTime.Now,
            BoshlanishVaqti = model.BoshlanishVaqti,
            TugashVaqti = DateTime.Now,
            JavoblarJson = System.Text.Json.JsonSerializer.Serialize(javoblar)
        };

        _context.ImtihonNatijalar.Add(natija);
        await _context.SaveChangesAsync();

        // Tangacha qo'shish (50% yuqori natijalar uchun)
        if (foiz >= 50)
        {
            var talabaUser = await _context.Foydalanuvchilar.FindAsync(talabaId);
            if (talabaUser != null)
            {
                // Imtihon uchun tangacha miqdori (masalan, 2000 so'm)
                const decimal imtihonTangachaMiqdori = 2000m;
                decimal qoshiladiganTangacha = imtihonTangachaMiqdori * foiz / 100;
                
                talabaUser.Tangacha += qoshiladiganTangacha;
                talabaUser.YangilanganVaqt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        if (otdimi && imtihon.SertifikatBeriladimi)
        {
            // Sertifikat funksiyasi serverdagi ImtihonController ichida, shu yerda faqat flag qaytaramiz
            _logger.LogInformation("Talaba {TalabaId} imtihon {ImtihonId} uchun sertifikatga ega bo'lishi mumkin.", talabaId, imtihon.Id);
        }

        return Ok(new
        {
            success = true,
            natijaId = natija.Id,
            umumiyBall,
            maksimalBall,
            foiz = Math.Round(foiz, 2),
            otdimi
        });
    }

    // 👤 Profil ma'lumotlari
    [HttpGet("profile")]
    [Authorize(Roles = "student")]
    public async Task<IActionResult> Profile()
    {
        var ctx = await GetStudentContextAsync();
        if (ctx == null) return Unauthorized();
        var talaba = ctx.Value.talaba;
        if (talaba == null) return NotFound();

        return Ok(new
        {
            talaba.Id,
            talaba.Ism,
            talaba.Familiya,
            talaba.OtasiningIsmi,
            talaba.Login,
            talaba.TelefonRaqam
        });
    }

    // 👤 Profil yangilash
    [HttpPost("profile")]
    [Authorize(Roles = "student")]
    public async Task<IActionResult> UpdateProfile([FromBody] StudentProfileUpdateRequest request)
    {
        var ctx = await GetStudentContextAsync();
        if (ctx == null) return Unauthorized();
        var talaba = ctx.Value.talaba;
        if (talaba == null) return NotFound(new { message = "Talaba topilmadi." });

        if (!string.IsNullOrWhiteSpace(request.TelefonRaqam))
        {
            talaba.TelefonRaqam = request.TelefonRaqam.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            if (string.IsNullOrWhiteSpace(request.OldPassword))
            {
                return BadRequest(new { message = "Eski parolni kiriting." });
            }

            var verify = _passwordHasher.VerifyHashedPassword(talaba, talaba.Parol, request.OldPassword.Trim());
            if (verify == PasswordVerificationResult.Failed)
            {
                return BadRequest(new { message = "Eski parol noto'g'ri." });
            }

            talaba.Parol = _passwordHasher.HashPassword(talaba, request.NewPassword.Trim());
        }

        talaba.YangilanganVaqt = DateTime.Now;
        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }

    // 📄 Imtihon natijasi tafsiloti (javoblar bilan)
    [HttpGet("results/{natijaId:int}")]
    [Authorize(Roles = "student")]
    public async Task<IActionResult> ExamResultDetail(int natijaId)
    {
        var ctx = await GetStudentContextAsync();
        if (ctx == null) return Unauthorized();
        var currentId = ctx.Value.talabaId;

        var natija = await _context.ImtihonNatijalar
            .Include(n => n.Imtihon)!.ThenInclude(i => i.Guruh)!.ThenInclude(g => g.Kurs)
            .Include(n => n.Talaba)
            .FirstOrDefaultAsync(n => n.Id == natijaId);

        if (natija == null) return NotFound(new { message = "Natija topilmadi." });
        if (natija.TalabaId != currentId) return Forbid();

        List<object> javoblar = new();
        if (!string.IsNullOrWhiteSpace(natija.JavoblarJson))
        {
            try
            {
                javoblar = JsonSerializer.Deserialize<List<object>>(natija.JavoblarJson) ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "JavoblarJson parse bo'lmadi, natijaId={NatijaId}", natijaId);
            }
        }

        return Ok(new
        {
            natija.Id,
            natija.ImtihonId,
            Imtihon = natija.Imtihon?.Nomi,
            Guruh = natija.Imtihon?.Guruh?.Nomi,
            Kurs = natija.Imtihon?.Guruh?.Kurs?.Nomi,
            natija.Sana,
            natija.UmumiyBall,
            natija.MaksimalBall,
            natija.FoizNatija,
            natija.Otdimi,
            natija.BoshlanishVaqti,
            natija.TugashVaqti,
            Javoblar = javoblar
        });
    }

    // 🎓 Sertifikatlar ro'yxati
    [HttpGet("certificates")]
    [Authorize(Roles = "student")]
    public async Task<IActionResult> Certificates()
    {
        var ctx = await GetStudentContextAsync();
        if (ctx == null) return Unauthorized();
        var talabaId = ctx.Value.talabaId;

        var list = await _context.Sertifikatlar
            .Include(s => s.Imtihon)
            .Include(s => s.Kurs)
            .Where(s => s.TalabaId == talabaId)
            .OrderByDescending(s => s.BerilganSana)
            .ToListAsync();

        return Ok(list.Select(s => new
        {
            s.Id,
            s.SertifikatRaqami,
            s.SertifikatNomi,
            s.BerilganSana,
            s.YaroqlilikMuddati,
            s.Ball,
            s.Foiz,
            s.Status,
            Imtihon = s.Imtihon?.Nomi,
            Kurs = s.Kurs?.Nomi
        }));
    }

    // 🎓 Sertifikat PDF yuklab olish
    [HttpGet("certificates/{id:int}/pdf")]
    [Authorize(Roles = "student")]
    public async Task<IActionResult> CertificatePdf(int id)
    {
        var ctx = await GetStudentContextAsync();
        if (ctx == null) return Unauthorized();
        var talabaId = ctx.Value.talabaId;

        var sertifikat = await _context.Sertifikatlar
            .Include(s => s.Talaba)
            .Include(s => s.Kurs)
            .Include(s => s.Imtihon)
            .FirstOrDefaultAsync(s => s.Id == id && s.TalabaId == talabaId);

        if (sertifikat == null)
        {
            return NotFound(new { message = "Sertifikat topilmadi." });
        }

        using var ms = new MemoryStream();
        var doc = new Document(PageSize.A4, 36, 36, 36, 36);
        PdfWriter.GetInstance(doc, ms);
        doc.Open();

        var title = new Paragraph("Sertifikat", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 20))
        {
            Alignment = Element.ALIGN_CENTER,
            SpacingAfter = 18f
        };
        doc.Add(title);

        void AddRow(string label, string value)
        {
            var p = new Paragraph($"{label} {value}", FontFactory.GetFont(FontFactory.HELVETICA, 12));
            p.SpacingAfter = 6f;
            doc.Add(p);
        }

        AddRow("Sertifikat raqami:", sertifikat.SertifikatRaqami);
        AddRow("F.I.Sh:", $"{sertifikat.Talaba?.Familiya} {sertifikat.Talaba?.Ism}".Trim());
        AddRow("Kurs:", sertifikat.Kurs?.Nomi ?? "-");
        AddRow("Imtihon:", sertifikat.Imtihon?.Nomi ?? "-");
        AddRow("Berilgan sana:", sertifikat.BerilganSana.ToString("yyyy-MM-dd"));
        if (sertifikat.YaroqlilikMuddati.HasValue)
        {
            AddRow("Yaroqlilik muddati:", sertifikat.YaroqlilikMuddati.Value.ToString("yyyy-MM-dd"));
        }
        AddRow("Ball:", sertifikat.Ball.ToString("0.##"));
        AddRow("Foiz:", sertifikat.Foiz.ToString("0.##"));
        AddRow("Status:", sertifikat.Status ?? "-");

        doc.Close();
        var bytes = ms.ToArray();
        return File(bytes, "application/pdf", $"Sertifikat_{sertifikat.SertifikatRaqami}.pdf");
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

    private async Task<(bool Eligible, string Message)> CheckAttendanceAsync(int talabaId, Imtihon imtihon)
    {
        var davomat = await _context.Davomatlar
            .Where(d => d.TalabaId == talabaId && d.GuruhId == imtihon.GuruhId && d.Sana.Date == imtihon.Sana.Date)
            .OrderByDescending(d => d.YangilanganVaqt)
            .FirstOrDefaultAsync();

        if (davomat == null)
        {
            return (false, "Bugungi dars uchun davomat hali belgilanmagan.");
        }

        if (!AllowedAttendanceStatuses.Contains(davomat.Holati ?? string.Empty))
        {
            return (false, $"Darsga kelgan talabalar imtihonni ishlashi mumkin. Sizning holatingiz: {davomat.Holati}");
        }

        return (true, string.Empty);
    }

    private static string NormalizeRole(string? role)
    {
        var normalized = role?.Trim().ToLower() ?? "student";
        return normalized switch
        {
            "oquvchi" => "student",
            "talaba" => "student",
            _ => normalized
        };
    }
}

public class LoginRequest
{
    public string Login { get; set; } = string.Empty;
    public string Parol { get; set; } = string.Empty;
}

public class StudentProfileUpdateRequest
{
    public string? TelefonRaqam { get; set; }
    public string? OldPassword { get; set; }
    public string? NewPassword { get; set; }
}

