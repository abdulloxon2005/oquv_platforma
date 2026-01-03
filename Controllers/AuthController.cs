using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using talim_platforma.Data;
using talim_platforma.Models;
using talim_platforma.Helpers;

namespace talim_platforma.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<Foydalanuvchi> _passwordHasher;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<Foydalanuvchi>();
        }

        // 🔹 Login sahifasi (Sayt uchun)
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // 🔹 Login (Sayt POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string login, string parol, string? deviceToken = null, string? platforma = "web")
        {
            // Check if this is an AJAX request
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(parol))
            {
                if (isAjax)
                {
                    return Json(new { success = false, message = "❌ Login va parolni kiriting!" });
                }
                ViewBag.Xato = "❌ Login va parolni kiriting!";
                return View();
            }

            var foydalanuvchi = _context.Foydalanuvchilar
                .FirstOrDefault(f => f.Login.ToLower().Trim() == login.ToLower().Trim());

            if (foydalanuvchi == null)
            {
                if (isAjax)
                {
                    return Json(new { success = false, message = "❌ Login yoki parol noto'g'ri." });
                }
                ViewBag.Xato = "❌ Login yoki parol noto'g'ri.";
                return View();
            }

            // Arxivlangan foydalanuvchini tekshirish
            if (foydalanuvchi.Arxivlanganmi && foydalanuvchi.ArxivlanganSana.HasValue)
            {
                var kunlarOtdi = (DateTime.Now - foydalanuvchi.ArxivlanganSana.Value).Days;
                
                // 1 oydan (30 kun) keyin kirishni bloklash
                if (kunlarOtdi >= 30)
                {
                    if (isAjax)
                    {
                        return Json(new { success = false, message = "❌ Sizning hisobingiz arxivlangan va 1 oy o'tgan. Kirish imkoni yo'q." });
                    }
                    ViewBag.Xato = "❌ Sizning hisobingiz arxivlangan va 1 oy o'tgan. Kirish imkoni yo'q.";
                    return View();
                }
                
                // 1 oy ichida bo'lsa, faqat o'z ma'lumotlarini ko'ra oladi (keyinroq middleware yoki filter bilan ta'minlanadi)
            }

            var result = _passwordHasher.VerifyHashedPassword(foydalanuvchi, foydalanuvchi.Parol, parol);
            if (result == PasswordVerificationResult.Failed)
            {
                if (isAjax)
                {
                    return Json(new { success = false, message = "❌ Login yoki parol noto'g'ri." });
                }
                ViewBag.Xato = "❌ Login yoki parol noto'g'ri.";
                return View();
            }

            bool dataChanged = false;
            // Qurilma tokeni (sayt uchun emas, lekin umumiy)
            if (!string.IsNullOrEmpty(deviceToken))
            {
                var mavjud = _context.Qurilmalar.FirstOrDefault(q =>
                    q.Token == deviceToken && q.FoydalanuvchiId == foydalanuvchi.Id);

                if (mavjud == null)
                {
                    _context.Qurilmalar.Add(new Qurilma
                    {
                        FoydalanuvchiId = foydalanuvchi.Id,
                        Token = deviceToken,
                        Platforma = platforma ?? "web",
                        OxirgiFoydalanish = DateTime.Now
                    });
                }
                else
                {
                    mavjud.OxirgiFoydalanish = DateTime.Now;
                }

                dataChanged = true;
            }

            var normalizedRole = RoleHelper.Normalize(foydalanuvchi.Rol);
            if (!string.Equals(foydalanuvchi.Rol, normalizedRole, StringComparison.OrdinalIgnoreCase))
            {
                foydalanuvchi.Rol = normalizedRole;
                dataChanged = true;
            }

            if (dataChanged)
            {
                _context.SaveChanges();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, foydalanuvchi.Id.ToString()),
                new Claim(ClaimTypes.Name, $"{foydalanuvchi.Familiya} {foydalanuvchi.Ism}".Trim()),
                new Claim(ClaimTypes.Role, normalizedRole)
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

            HttpContext.Session.SetString("FoydalanuvchiId", foydalanuvchi.Id.ToString());
            HttpContext.Session.SetString("Rol", normalizedRole);

            // Rollarga qarab yo'naltirish
            var rol = foydalanuvchi.Rol.Trim().ToLower();
            string redirectUrl = rol switch
            {
                "admin" => "/Admin/Foydalanuvchilar",
                "oqituvchi" or "teacher" => "/Oqituvchi/Index",
                "oquvchi" or "student" => "/Oquvchi/DashboardTalaba",
                _ => "/Home/Index"
            };

            if (isAjax)
            {
                return Json(new { success = true, redirect = redirectUrl });
            }

            return RedirectToAction(redirectUrl.Split('/')[1], redirectUrl.Split('/')[0]);
        }

        // 🚪 Logout
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // 🔍 Login mavjudligini tekshirish (Remote uchun)
        [AcceptVerbs("GET", "POST")]
        public IActionResult IsLoginAvailable(string login)
        {
            if (_context.Foydalanuvchilar.Any(x => x.Login.ToLower() == login.ToLower().Trim()))
            {
                return Json($"Bu login allaqachon band");
            }
            return Json(true);
        }

  
    }
}      