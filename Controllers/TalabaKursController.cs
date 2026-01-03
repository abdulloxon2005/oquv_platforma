using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using talim_platforma.Data;
using talim_platforma.Models;

namespace talim_platforma.Controllers
{
    [Authorize(Roles = "admin")]
    [AutoValidateAntiforgeryToken]
    public class TalabaKursController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TalabaKursController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Index
        public async Task<IActionResult> Index()
        {
            var boglanmalar = await _context.TalabaKurslar
                .Include(tk => tk.Talaba)
                .Include(tk => tk.Kurs)
                .ToListAsync();

            return View(boglanmalar);
        }

        // 2. Yaratish (GET)
        public IActionResult Yaratish()
        {
            ViewBag.Talabalar = new SelectList(
                _context.Foydalanuvchilar.Where(f => f.Rol.ToLower() == "student"),
                "Id", "Ism");

            ViewBag.Kurslar = new MultiSelectList(
                _context.Kurslar, "Id", "Nomi");

            return View();
        }

        // 2. Yaratish (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Yaratish(int talabaId, List<int> kurslar)
        {
            if (!ModelState.IsValid || kurslar == null || !kurslar.Any())
            {
                ModelState.AddModelError("", "Kamida bitta kurs tanlanishi kerak.");

                ViewBag.Talabalar = new SelectList(
                    _context.Foydalanuvchilar.Where(f => f.Rol.ToLower() == "student"),
                    "Id", "Ism", talabaId);

                ViewBag.Kurslar = new MultiSelectList(
                    _context.Kurslar, "Id", "Nomi", kurslar);

                return View();
            }

            foreach (var kursId in kurslar)
            {
                bool mavjud = await _context.TalabaKurslar
                    .AnyAsync(tk => tk.TalabaId == talabaId && tk.KursId == kursId);

                if (!mavjud)
                {
                    _context.TalabaKurslar.Add(new TalabaKurs
                    {
                        TalabaId = talabaId,
                        KursId = kursId,
                        Holati = "Faol"
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["xabar"] = "Muvaffaqiyatli saqlandi!";
            return RedirectToAction(nameof(Index));
        }

        // ✅ Yangi metod: Talabani qidirish (AJAX)
        [HttpGet]
        public IActionResult SearchTalaba(string q)
        {
            if (string.IsNullOrEmpty(q))
                return Json(new List<object>());

            var talabalar = _context.Foydalanuvchilar
                .Where(f => f.Rol.ToLower() == "student" && f.Ism.Contains(q))
                .Select(f => new { id = f.Id, text = f.Ism })
                .ToList();

            return Json(talabalar);
        }

        // 3. Tahrirlash (GET)
        public async Task<IActionResult> Tahrirlash(int id)
        {
            var tk = await _context.TalabaKurslar.FindAsync(id);
            if (tk == null) return NotFound();

            ViewBag.Talabalar = new SelectList(
                _context.Foydalanuvchilar.Where(f => f.Rol.ToLower() == "student"),
                "Id", "Ism", tk.TalabaId);

            ViewBag.Kurslar = new SelectList(
                _context.Kurslar, "Id", "Nomi", tk.KursId);

            return View(tk);
        }

        // 3. Tahrirlash (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Tahrirlash(int id, TalabaKurs model)
        {
            if (id != model.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Talabalar = new SelectList(
                    _context.Foydalanuvchilar.Where(f => f.Rol.ToLower() == "student"),
                    "Id", "Ism", model.TalabaId);

                ViewBag.Kurslar = new SelectList(
                    _context.Kurslar, "Id", "Nomi", model.KursId);

                return View(model);
            }

            var tk = await _context.TalabaKurslar.FindAsync(id);
            if (tk == null) return NotFound();

            tk.TalabaId = model.TalabaId;
            tk.KursId = model.KursId;
            tk.Holati = model.Holati;

            await _context.SaveChangesAsync();
            TempData["xabar"] = "O‘zgarishlar saqlandi!";
            return RedirectToAction(nameof(Index));
        }

        // 4. Ochirish
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ochirish(int id)
        {
            var boglanma = await _context.TalabaKurslar.FindAsync(id);
            if (boglanma == null) return NotFound();

            _context.TalabaKurslar.Remove(boglanma);
            await _context.SaveChangesAsync();

            TempData["xabar"] = "Bog‘lanma o‘chirildi!";
            return RedirectToAction(nameof(Index));
        }
    }
}
