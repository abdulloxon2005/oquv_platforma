using System;
using System.Linq;
using System.Security.Claims;
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
    public class DavomatController : Controller
    {
        private static readonly string[] ValidStatuses = { "Keldi", "Kelmadi", "Kech keldi", "Uzrli" };

        private readonly ApplicationDbContext _context;
        private readonly IStudentStatusService _studentStatusService;
        private int? _currentUserId;

        public DavomatController(ApplicationDbContext context, IStudentStatusService studentStatusService)
        {
            _context = context;
            _studentStatusService = studentStatusService;
        }

        private int? GetCurrentUserId()
        {
            if (!_currentUserId.HasValue)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                {
                    _currentUserId = userId;
                }
            }
            return _currentUserId;
        }

        private string GetCurrentRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? "";
        }

        private bool IsAdminRole()
        {
            return GetCurrentRole() == "admin";
        }

        public IActionResult Index(int? guruhId)
        {
            var query = _context.Davomatlar
                .Include(d => d.Guruh)
                .Include(d => d.Talaba)
                .Include(d => d.Dars)
                .OrderByDescending(d => d.Sana)
                .AsQueryable();

            // Faqat o'z guruhlarini ko'rish (teacher uchun)
            if (!IsAdminRole())
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId.HasValue)
                {
                    query = query.Where(d => d.OqituvchiId == currentUserId.Value);
                }
            }

            if (guruhId.HasValue)
            {
                query = query.Where(d => d.GuruhId == guruhId.Value);
            }

            var records = query.Take(200).ToList();
            
            // Guruhlar ro'yxatini ham filterlash
            var guruhQuery = _context.Guruhlar.AsQueryable();
            if (!IsAdminRole())
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId.HasValue)
                {
                    guruhQuery = guruhQuery.Where(g => g.OqituvchiId == currentUserId.Value);
                }
            }
            ViewBag.Guruhlar = new SelectList(guruhQuery.ToList(), "Id", "Nomi", guruhId);

            return View(records);
        }

        [HttpGet]
        public IActionResult Create(int? guruhId)
        {
            var model = new DavomatCreateViewModel
            {
                GuruhId = guruhId ?? 0,
                Sana = DateTime.Today
            };

            PopulateSelectLists(guruhId, null);
            return View("Upsert", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DavomatCreateViewModel model)
        {
            if (!ValidStatuses.Contains(model.Holati))
            {
                ModelState.AddModelError(nameof(model.Holati), "Yaroqsiz holat tanlandi.");
            }

            var guruh = await _context.Guruhlar.Include(g => g.Darslar).FirstOrDefaultAsync(g => g.Id == model.GuruhId);
            if (guruh == null)
            {
                ModelState.AddModelError(nameof(model.GuruhId), "Guruh topilmadi.");
            }

            // Guruhga ruxsat borligini tekshirish
            if (!IsAdminRole())
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue || guruh?.OqituvchiId != currentUserId.Value)
                {
                    ModelState.AddModelError("", "Bu guruhga davomat qo'shish huquqingiz yo'q.");
                }
            }

            if (!ModelState.IsValid)
            {
                PopulateSelectLists(model.GuruhId, model.TalabaId);
                return View("Upsert", model);
            }

            var davomat = new Davomat
            {
                GuruhId = model.GuruhId,
                TalabaId = model.TalabaId,
                DarsId = model.DarsId,
                OqituvchiId = guruh!.OqituvchiId,
                Sana = model.Sana,
                Holati = model.Holati,
                Izoh = model.Izoh,
                YaratilganVaqt = DateTime.Now,
                YangilanganVaqt = DateTime.Now
            };

            _context.Davomatlar.Add(davomat);
            await _context.SaveChangesAsync();
            await _studentStatusService.RefreshStudentStatusAsync(model.TalabaId, model.GuruhId);

            TempData["success"] = "Davomat yozuvi qo‘shildi.";
            return RedirectToAction(nameof(Index), new { guruhId = model.GuruhId });
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var entity = _context.Davomatlar
                .Include(d => d.Guruh)
                .FirstOrDefault(d => d.Id == id);
            
            if (entity == null)
            {
                return NotFound();
            }

            // Ruxsatni tekshirish
            if (!IsAdminRole())
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue || entity.OqituvchiId != currentUserId.Value)
                {
                    return Forbid();
                }
            }

            var model = new DavomatCreateViewModel
            {
                Id = entity.Id,
                GuruhId = entity.GuruhId,
                TalabaId = entity.TalabaId,
                DarsId = entity.DarsId,
                Sana = entity.Sana,
                Holati = entity.Holati,
                Izoh = entity.Izoh
            };

            PopulateSelectLists(entity.GuruhId, entity.TalabaId);
            return View("Upsert", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DavomatCreateViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ValidStatuses.Contains(model.Holati))
            {
                ModelState.AddModelError(nameof(model.Holati), "Yaroqsiz holat tanlandi.");
            }

            if (!ModelState.IsValid)
            {
                PopulateSelectLists(model.GuruhId, model.TalabaId);
                return View("Upsert", model);
            }

            var entity = await _context.Davomatlar
                .Include(d => d.Guruh)
                .FirstOrDefaultAsync(d => d.Id == id);
            
            if (entity == null)
            {
                return NotFound();
            }

            // Ruxsatni tekshirish
            if (!IsAdminRole())
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue || entity.OqituvchiId != currentUserId.Value)
                {
                    return Forbid();
                }
            }

            entity.GuruhId = model.GuruhId;
            entity.TalabaId = model.TalabaId;
            entity.DarsId = model.DarsId;
            entity.Sana = model.Sana;
            entity.Holati = model.Holati;
            entity.Izoh = model.Izoh;
            entity.YangilanganVaqt = DateTime.Now;

            await _context.SaveChangesAsync();
            await _studentStatusService.RefreshStudentStatusAsync(model.TalabaId, model.GuruhId);

            TempData["success"] = "Davomat yangilandi.";
            return RedirectToAction(nameof(Index), new { guruhId = model.GuruhId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.Davomatlar
                .Include(d => d.Guruh)
                .FirstOrDefaultAsync(d => d.Id == id);
            
            if (entity == null)
            {
                return NotFound();
            }

            // Ruxsatni tekshirish
            if (!IsAdminRole())
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue || entity.OqituvchiId != currentUserId.Value)
                {
                    return Forbid();
                }
            }

            _context.Davomatlar.Remove(entity);
            await _context.SaveChangesAsync();
            await _studentStatusService.RefreshStudentStatusAsync(entity.TalabaId, entity.GuruhId);

            TempData["success"] = "Davomat o‘chirildi.";
            return RedirectToAction(nameof(Index), new { guruhId = entity.GuruhId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReason([FromBody] ReasonRequest request)
        {
            if (request == null || request.DavomatId <= 0 || string.IsNullOrWhiteSpace(request.Sabab))
            {
                return BadRequest(new { message = "Noto'g'ri ma'lumot." });
            }

            var davomat = await _context.Davomatlar
                .Include(d => d.Guruh)
                .FirstOrDefaultAsync(d => d.Id == request.DavomatId);
            
            if (davomat == null)
            {
                return NotFound();
            }

            // Ruxsatni tekshirish
            if (!IsAdminRole())
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue || davomat.OqituvchiId != currentUserId.Value)
                {
                    return Forbid();
                }
            }

            if (await _context.DavomatSabablar.AnyAsync(ds => ds.DavomatId == davomat.Id))
            {
                return BadRequest(new { message = "Bu davomat uchun sabab allaqachon mavjud." });
            }

            var sabab = new DavomatSabab
            {
                DavomatId = davomat.Id,
                TalabaId = davomat.TalabaId,
                Sabab = request.Sabab.Trim(),
                Sana = DateTime.Now
            };

            _context.DavomatSabablar.Add(sabab);
            await _context.SaveChangesAsync();

            await _studentStatusService.RefreshStudentStatusAsync(davomat.TalabaId, davomat.GuruhId);

            return Json(new { success = true });
        }

        private void PopulateSelectLists(int? guruhId, int? talabaId)
        {
            // Guruhlar ro'yxatini filterlash
            var guruhQuery = _context.Guruhlar.AsQueryable();
            if (!IsAdminRole())
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId.HasValue)
                {
                    guruhQuery = guruhQuery.Where(g => g.OqituvchiId == currentUserId.Value);
                }
            }
            ViewBag.Guruhlar = new SelectList(guruhQuery.ToList(), "Id", "Nomi", guruhId);

            var talabaQuery = _context.Foydalanuvchilar
                .Where(f => f.Rol == "Talaba" || f.Rol.ToLower() == "student");
            if (guruhId.HasValue && guruhId.Value > 0)
            {
                var talabaIds = _context.TalabaGuruhlar
                    .Where(tg => tg.GuruhId == guruhId.Value)
                    .Select(tg => tg.TalabaId)
                    .ToList();

                talabaQuery = talabaQuery.Where(f => talabaIds.Contains(f.Id));
            }
            ViewBag.Talabalar = new SelectList(talabaQuery.ToList(), "Id", "Ism", talabaId);

            var darsQuery = _context.Darslar.AsQueryable();
            if (guruhId.HasValue && guruhId.Value > 0)
            {
                darsQuery = darsQuery.Where(d => d.GuruhId == guruhId.Value);
            }
            ViewBag.Darslar = new SelectList(darsQuery.ToList(), "Id", "Mavzu");
        }

        public class ReasonRequest
        {
            public int DavomatId { get; set; }
            public string Sabab { get; set; }
        }
    }
}

