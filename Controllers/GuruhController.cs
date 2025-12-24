using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using talim_platforma.Data;
using talim_platforma.Models;
using talim_platforma.Models.ViewModels;
using talim_platforma.Services;

namespace talim_platforma.Controllers
{
    [Authorize(Roles = "teacher,admin")]
    [AutoValidateAntiforgeryToken]
    public class GuruhController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IStudentStatusService _studentStatusService;

        public GuruhController(ApplicationDbContext context, IStudentStatusService studentStatusService)
        {
            _context = context;
            _studentStatusService = studentStatusService;
        }

        // 📋 Guruhlar ro‘yxati
        public IActionResult Guruhlar()
        {
            var guruhlar = _context.Guruhlar
                .Include(g => g.Kurs)
                .Include(g => g.Oqituvchi)
                .Include(g => g.TalabaGuruhlar)
                    .ThenInclude(tg => tg.Talaba)
                .OrderByDescending(g => g.YaratilganVaqt)
                .ToList();

            return View(guruhlar);
        }

        // ➕ Guruh qo‘shish (GET)
        [HttpGet]
        public IActionResult GuruhQoshish()
        {
            ViewBag.Kurslar = new SelectList(_context.Kurslar.ToList(), "Id", "Nomi");
            ViewBag.Oqituvchilar = new SelectList(_context.Foydalanuvchilar
                .Where(f => f.Rol == "Teacher" || f.Rol.ToLower() == "teacher")
                .ToList(), "Id", "Ism");

            // Dars kunlari varianti
            ViewBag.DarsKunlari = new List<SelectListItem>
            {
                new SelectListItem { Value = "Du,Cho,Ju", Text = "Du, Cho, Ju" },
                new SelectListItem { Value = "Se,Pay,Shan", Text = "Se, Pay, Shan" }
            };

            return View();
        }

        // ➕ Guruh qo‘shish (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GuruhQoshish(Guruh model)
        {
            // Guruh nomi takrorlanmasligi uchun tekshirish
            if (_context.Guruhlar.Any(g => g.Nomi == model.Nomi))
            {
                ModelState.AddModelError("Nomi", "Bu nomdagi guruh allaqachon mavjud!");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    model.YaratilganVaqt = DateTime.Now;
                    model.YangilanganVaqt = DateTime.Now;

                    _context.Guruhlar.Add(model);
                    _context.SaveChanges();

                    Console.WriteLine("✅ Guruh bazaga muvaffaqiyatli qo‘shildi: " + model.Nomi);
                    TempData["success"] = "Guruh muvaffaqiyatli qo‘shildi!";
                    return RedirectToAction("Guruhlar");
                }
                catch (Exception ex)
                {
                    // ❌ Xatoni konsolga chiqaramiz
                    Console.WriteLine("❌ Guruh saqlashda xatolik: " + ex.Message);
                    Console.WriteLine("📄 StackTrace: " + ex.StackTrace);

                    TempData["error"] = "Xatolik yuz berdi: " + ex.Message;
                }
            }
            else
            {
                Console.WriteLine("⚠️ Modelda xatolik bor yoki ma’lumot to‘liq kiritilmagan.");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine("➡️ " + error.ErrorMessage);
                }

                TempData["error"] = "Ma’lumotlar to‘liq kiritilmadi!";
            }

            // Dropdownlarni qayta yuklash (xato bo‘lsa)
            ViewBag.Kurslar = new SelectList(_context.Kurslar.ToList(), "Id", "Nomi", model.KursId);
            ViewBag.Oqituvchilar = new SelectList(_context.Foydalanuvchilar
                .Where(f => f.Rol == "Teacher" || f.Rol.ToLower() == "teacher")
                .ToList(), "Id", "Ism", model.OqituvchiId);

            ViewBag.DarsKunlari = new List<SelectListItem>
            {
                new SelectListItem { Value = "Du,Cho,Ju", Text = "Du, Cho, Ju" },
                new SelectListItem { Value = "Se,Pay,Shan", Text = "Se, Pay, Shan" }
            };

            return View(model);
        }



        // ✏️ Guruh tahrirlash (GET)
        [HttpGet]
        public IActionResult GuruhTahrirlash(int id)
        {
            var guruh = _context.Guruhlar.Find(id);
            if (guruh == null)
                return NotFound();

            ViewBag.Kurslar = new SelectList(_context.Kurslar.ToList(), "Id", "Nomi", guruh.KursId);
            ViewBag.Oqituvchilar = new SelectList(_context.Foydalanuvchilar
                .Where(f => f.Rol == "Teacher" || f.Rol.ToLower() == "teacher")
                .ToList(), "Id", "Ism", guruh.OqituvchiId);

            ViewBag.DarsKunlari = new List<SelectListItem>
            {
                new SelectListItem { Value = "Du,Cho,Ju", Text = "Du, Cho, Ju" },
                new SelectListItem { Value = "Se,Pay,Shan", Text = "Se, Pay, Shan" }
            };

            return View(guruh);
        }

        // ✏️ Guruh tahrirlash (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GuruhTahrirlash(Guruh model)
        {
            if (_context.Guruhlar.Any(g => g.Nomi == model.Nomi && g.Id != model.Id))
            {
                ModelState.AddModelError("Nomi", "Bu nomdagi guruh allaqachon mavjud!");
            }

            if (ModelState.IsValid)
            {
                model.YangilanganVaqt = DateTime.Now;

                _context.Guruhlar.Update(model);
                _context.SaveChanges();
                return RedirectToAction("Guruhlar");
            }

            ViewBag.Kurslar = new SelectList(_context.Kurslar.ToList(), "Id", "Nomi", model.KursId);
            ViewBag.Oqituvchilar = new SelectList(_context.Foydalanuvchilar
                .Where(f => f.Rol == "Teacher" || f.Rol.ToLower() == "teacher")
                .ToList(), "Id", "Ism", model.OqituvchiId);

            ViewBag.DarsKunlari = new List<SelectListItem>
            {
                new SelectListItem { Value = "Du,Cho,Ju", Text = "Du, Cho, Ju" },
                new SelectListItem { Value = "Se,Pay,Shan", Text = "Se, Pay, Shan" }
            };

            return View(model);
        }

        // 🗑️ Guruh o‘chirish (GET)
        [HttpGet]
        public IActionResult GuruhOchirish(int id)
        {
            var guruh = _context.Guruhlar
                .Include(g => g.Kurs)
                .Include(g => g.Oqituvchi)
                .FirstOrDefault(g => g.Id == id);

            if (guruh == null)
                return NotFound();

            return View(guruh);
        }

        // 🗑️ Guruh o‘chirish (POST)
        [HttpPost, ActionName("GuruhOchirish")]
        [ValidateAntiForgeryToken]
        public IActionResult GuruhOchirishTasdiqlandi(int id)
        {
            var guruh = _context.Guruhlar.Find(id);
            if (guruh != null)
            {
                _context.Guruhlar.Remove(guruh);
                _context.SaveChanges();
            }
            return RedirectToAction("Guruhlar");
        }

        public IActionResult SearchStudent(string query, int guruhId)
        {
            var talabaIds = _context.TalabaGuruhlar
                                    .Where(tg => tg.GuruhId == guruhId)
                                    .Select(tg => tg.TalabaId)
                                    .ToList();

            var talabalarQuery = _context.Foydalanuvchilar
                .Where(f => (f.Rol == "Talaba" || f.Rol.ToLower() == "student") && !talabaIds.Contains(f.Id))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim().ToLower();
                talabalarQuery = talabalarQuery.Where(f =>
                    (f.Ism ?? string.Empty).ToLower().Contains(term) ||
                    (f.Login ?? string.Empty).ToLower().Contains(term) ||
                    (f.Familiya ?? string.Empty).ToLower().Contains(term));
            }

            var talabalar = talabalarQuery.ToList();

            return PartialView("_StudentSearchResults", talabalar);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStudentToGroup([FromBody] AddStudentRequest request)
        {
            if (request == null || request.TalabaId <= 0 || request.GuruhId <= 0)
            {
                return Json(new { success = false, message = "Ma'lumot xato." });
            }

            if (await _context.TalabaGuruhlar.AnyAsync(tg => tg.TalabaId == request.TalabaId && tg.GuruhId == request.GuruhId))
                return Json(new { success = false, message = "Talaba allaqachon guruhda!" });

            var talabaGuruh = new TalabaGuruh
            {
                TalabaId = request.TalabaId,
                GuruhId = request.GuruhId,
                QoshilganSana = DateTime.Now
            };

            _context.TalabaGuruhlar.Add(talabaGuruh);
            await _context.SaveChangesAsync();
            await _studentStatusService.RefreshStudentStatusAsync(request.TalabaId, request.GuruhId);

            return Json(new { success = true, message = "Talaba muvaffaqiyatli qo‘shildi!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveStudentFromGroup(int talabaId, int guruhId)
        {
            var entity = _context.TalabaGuruhlar.FirstOrDefault(tg => tg.TalabaId == talabaId && tg.GuruhId == guruhId);
            if (entity != null)
            {
                _context.TalabaGuruhlar.Remove(entity);
                _context.SaveChanges();
            }

            return RedirectToAction(nameof(Talabalar), new { id = guruhId });
        }

        public async Task<IActionResult> Talabalar(int id)
        {
            var guruh = await _context.Guruhlar
                .Include(g => g.Kurs)
                .Include(g => g.TalabaGuruhlar)
                    .ThenInclude(tg => tg.Talaba)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (guruh == null)
            {
                return NotFound();
            }

            await _studentStatusService.RefreshGroupStatusesAsync(id);

            var talabaIds = guruh.TalabaGuruhlar?.Select(tg => tg.TalabaId).ToList() ?? new List<int>();
            var oxirgiDavomatlar = _context.Davomatlar
                .Where(d => d.GuruhId == id && talabaIds.Contains(d.TalabaId))
                .OrderByDescending(d => d.Sana)
                .ToList();
            var sabablanganDavomatlar = _context.DavomatSabablar
                .Where(ds => oxirgiDavomatlar.Select(d => d.Id).Contains(ds.DavomatId))
                .Select(ds => ds.DavomatId)
                .ToHashSet();

            var model = new GuruhTalabalarViewModel
            {
                GuruhId = guruh.Id,
                GuruhNomi = guruh.Nomi,
                KursNomi = guruh.Kurs?.Nomi ?? "-"
            };

            foreach (var tg in guruh.TalabaGuruhlar ?? new List<TalabaGuruh>())
            {
                var lastRecord = oxirgiDavomatlar.FirstOrDefault(d => d.TalabaId == tg.TalabaId);
                var unresolvedAbsence = oxirgiDavomatlar
                    .Where(d => d.TalabaId == tg.TalabaId && d.Holati == "Kelmadi")
                    .OrderByDescending(d => d.Sana)
                    .FirstOrDefault(d => !sabablanganDavomatlar.Contains(d.Id));

                model.Talabalar.Add(new TalabaStatusItem
                {
                    TalabaId = tg.TalabaId,
                    Ism = tg.Talaba?.Ism ?? string.Empty,
                    Familiya = tg.Talaba?.Familiya ?? string.Empty,
                    Telefon = tg.Talaba?.TelefonRaqam,
                    Faolmi = tg.Talaba?.Faolmi ?? true,
                    OxirgiHolat = lastRecord?.Holati,
                    OxirgiSana = lastRecord?.Sana,
                    DavomatIdSababUchun = unresolvedAbsence?.Id
                });
            }

            return View(model);
        }

        public class AddStudentRequest
        {
            public int TalabaId { get; set; }
            public int GuruhId { get; set; }
        }



    }
}
