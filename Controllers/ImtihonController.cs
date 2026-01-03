using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using talim_platforma.Data;
using talim_platforma.Models;
using talim_platforma.Models.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using talim_platforma.Helpers;
using System.Text;
using UglyToad.PdfPig;
using WordParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;

namespace talim_platforma.Controllers
{
    [Authorize(Roles = "teacher,admin")]
    [AutoValidateAntiforgeryToken]
    public class ImtihonController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ImtihonController> _logger;
        private int? _currentUserId;
        private string? _currentRole;
        private static readonly HashSet<string> YaroqliDavomatHolatlari =
            new(StringComparer.OrdinalIgnoreCase) { "keldi", "kech keldi" };

        public ImtihonController(ApplicationDbContext context, ILogger<ImtihonController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // 📋 Barcha imtihonlar ro'yxati
        [HttpGet]
        public async Task<IActionResult> Index(int? guruhId, string? format)
        {
            try
            {
                var query = _context.Imtihonlar
                    .Include(i => i.Guruh)
                        .ThenInclude(g => g.Kurs)
                    .Include(i => i.Oqituvchi)
                    .Include(i => i.Natijalar)
                    .AsQueryable();

                if (GetCurrentRole() == "teacher")
                {
                    var oqituvchiId = GetCurrentUserId();
                    if (oqituvchiId.HasValue)
                    {
                        query = query.Where(i => i.OqituvchiId == oqituvchiId.Value);
                    }
                }

                if (guruhId.HasValue)
                {
                    query = query.Where(i => i.GuruhId == guruhId.Value);
                }

                if (!string.IsNullOrEmpty(format))
                {
                    query = query.Where(i => i.ImtihonFormati == format);
                }

                var imtihonlar = await query
                    .OrderByDescending(i => i.Sana)
                    .ThenByDescending(i => i.YaratilganVaqt)
                    .ToListAsync();

                var guruhQuery = _context.Guruhlar
                    .Include(g => g.Kurs)
                    .AsQueryable();

                var currentUserId = GetCurrentUserId();
                if (GetCurrentRole() == "teacher" && currentUserId.HasValue)
                {
                    guruhQuery = guruhQuery.Where(g => g.OqituvchiId == currentUserId.Value);
                }

                var guruhlarList = await guruhQuery
                    .OrderBy(g => g.Nomi)
                    .ToListAsync();

                var guruhlarSelectList = new SelectList(
                    guruhlarList,
                    "Id",
                    "Nomi",
                    guruhId);

                ViewBag.Guruhlar = guruhlarSelectList;
                ViewBag.guruhId = guruhlarSelectList; // DropDownList helper uchun
                ViewBag.GuruhlarListFull = guruhlarList;

                var formatlarSelectList = new SelectList(
                    new[] { new { Value = "Online", Text = "Online" }, new { Value = "Offline", Text = "Offline" } },
                    "Value",
                    "Text",
                    format);

                ViewBag.Formatlar = formatlarSelectList;
                ViewBag.format = formatlarSelectList; // DropDownList helper uchun

                return View(imtihonlar);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Imtihonlar ro'yxatini yuklashda xatolik: {Message}", ex.Message);
                TempData["ErrorMessage"] = $"Xatolik yuz berdi: {ex.Message}";
                // Exception bo'lsa ham View'ni ko'rsatishga harakat qilamiz
                return View(new List<Imtihon>());
            }
        }

        // ➕ Imtihon yaratish (GET)
        [HttpGet]
        [Authorize(Roles = "teacher,admin")]
        public async Task<IActionResult> Yaratish()
        {
            try
            {
                if (!IsTeacherOrAdmin())
                {
                    return Forbid();
                }

                var model = new ImtihonCreateViewModel
                {
                    Sana = DateTime.Now,
                    ImtihonFormati = "Offline",
                    MinimalBall = 60
                };

                var oqituvchiId = GetCurrentUserId();
                if (GetCurrentRole() == "teacher" && !oqituvchiId.HasValue)
                {
                    TempData["ErrorMessage"] = "O'qituvchi ma'lumotlari topilmadi. Iltimos, qayta kirib keling.";
                    return RedirectToAction("Login", "Auth");
                }

                var guruhQuery = _context.Guruhlar
                    .Include(g => g.Kurs)
                    .AsQueryable();

                if (GetCurrentRole() == "teacher" && oqituvchiId.HasValue)
                {
                    guruhQuery = guruhQuery.Where(g => g.OqituvchiId == oqituvchiId.Value);
                }

                var guruhlar = await guruhQuery.ToListAsync();

                model.Guruhlar = guruhlar.Select(g => new SelectListItem
                {
                    Value = g.Id.ToString(),
                    Text = $"{g.Nomi} ({g.Kurs?.Nomi})"
                }).ToList();

                model.ImtihonTurlari = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Haftalik", Text = "Haftalik" },
                    new SelectListItem { Value = "Oylik", Text = "Oylik" },
                    new SelectListItem { Value = "Yakuniy", Text = "Yakuniy" },
                    new SelectListItem { Value = "Oraliq", Text = "Oraliq" }
                };

                model.ImtihonFormatlari = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Online", Text = "Online" },
                    new SelectListItem { Value = "Offline", Text = "Offline", Selected = true }
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Imtihon yaratish sahifasini yuklashda xatolik");
                TempData["ErrorMessage"] = "Xatolik yuz berdi.";
                return RedirectToAction("Index");
            }
        }

        // ➕ Imtihon yaratish (POST)
        [HttpPost]
        [Authorize(Roles = "teacher,admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Yaratish(ImtihonCreateViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await PopulateSelectListsAsync(model);
                    return View(model);
                }

                if (!IsTeacherOrAdmin())
                {
                    return Forbid();
                }

                var oqituvchiId = GetCurrentUserId();
                if (GetCurrentRole() == "teacher" && !oqituvchiId.HasValue)
                {
                    TempData["ErrorMessage"] = "O'qituvchi ma'lumotlari topilmadi. Iltimos, qayta kirib keling.";
                    return RedirectToAction("Login", "Auth");
                }

                // Guruhni tekshirish
                var guruh = await _context.Guruhlar
                    .Include(g => g.Kurs)
                    .FirstOrDefaultAsync(g => g.Id == model.GuruhId);

                if (guruh == null)
                {
                    ModelState.AddModelError("GuruhId", "Guruh topilmadi.");
                    await PopulateSelectListsAsync(model);
                    return View(model);
                }

                if (GetCurrentRole() == "teacher" && oqituvchiId.HasValue && guruh.OqituvchiId != oqituvchiId.Value)
                {
                    ModelState.AddModelError("", "Bu guruhga imtihon yaratish huquqingiz yo'q.");
                    await PopulateSelectListsAsync(model);
                    return View(model);
                }

                var examOwnerId = GetCurrentRole() == "teacher"
                    ? oqituvchiId!.Value
                    : guruh.OqituvchiId;

                var imtihon = new Imtihon
                {
                    GuruhId = model.GuruhId,
                    OqituvchiId = examOwnerId,
                    Nomi = model.Nomi,
                    ImtihonTuri = model.ImtihonTuri,
                    ImtihonFormati = model.ImtihonFormati,
                    Sana = model.Sana,
                    BoshlanishVaqti = model.BoshlanishVaqti,
                    TugashVaqti = model.TugashVaqti,
                    MuddatDaqiqada = model.MuddatDaqiqada,
                    MinimalBall = model.MinimalBall,
                    SertifikatBeriladimi = model.SertifikatBeriladimi,
                    Izoh = model.Izoh,
                    Faolmi = true
                };

                _context.Imtihonlar.Add(imtihon);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Imtihon muvaffaqiyatli yaratildi.";
                // Faqat saqlash: imtihon tafsilotiga qaytaramiz
                return RedirectToAction("Tafsilot", new { id = imtihon.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Imtihon yaratishda xatolik");
                TempData["ErrorMessage"] = "Imtihon yaratishda xatolik yuz berdi: " + ex.Message;
                await PopulateSelectListsAsync(model);
                return View(model);
            }
        }

        // 📝 Savollar qo'shish (GET)
        [HttpGet]
        [Authorize(Roles = "teacher,admin")]
        public async Task<IActionResult> SavollarQoshish(int id)
        {
            try
            {
                if (!IsTeacherOrAdmin())
                {
                    return Forbid();
                }

                var currentUserId = GetCurrentUserId();
                if (GetCurrentRole() == "teacher" && !currentUserId.HasValue)
                {
                    TempData["ErrorMessage"] = "O'qituvchi ma'lumotlari topilmadi.";
                    return RedirectToAction("Login", "Auth");
                }

                var imtihon = await _context.Imtihonlar
                    .Include(i => i.Guruh)
                    .Include(i => i.Savollar)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (imtihon == null)
                {
                    TempData["ErrorMessage"] = "Imtihon topilmadi.";
                    return RedirectToAction("Index");
                }

                if (!IsAdminRole() && currentUserId.HasValue && imtihon.OqituvchiId != currentUserId.Value)
                {
                    return Forbid();
                }

                ViewBag.Imtihon = imtihon;
                ViewBag.Savollar = imtihon.Savollar?.ToList() ?? new List<ImtihonSavol>();

                // View modeli int (imtihon Id) bo'lgani uchun, id ni model sifatida uzatamiz
                return View(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Savollar qo'shish sahifasini yuklashda xatolik");
                TempData["ErrorMessage"] = "Xatolik yuz berdi.";
                return RedirectToAction("Index");
            }
        }

        // 📝 Savol qo'shish (AJAX POST)
        [HttpPost]
        [Authorize(Roles = "teacher,admin")]
        public async Task<IActionResult> SavolQoshish([FromBody] ImtihonSavolViewModel model)
        {
            try
            {
                if (!IsTeacherOrAdmin())
                {
                    return Json(new { success = false, message = "Ruxsat etilmagan amal." });
                }

                var currentUserId = GetCurrentUserId();
                if (GetCurrentRole() == "teacher" && !currentUserId.HasValue)
                {
                    return Json(new { success = false, message = "O'qituvchi ma'lumotlari topilmadi." });
                }

                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Ma'lumotlar to'liq kiritilmagan." });
                }

                var imtihon = await _context.Imtihonlar.FindAsync(model.ImtihonId);
                if (imtihon == null)
                {
                    return Json(new { success = false, message = "Imtihon topilmadi." });
                }

                 if (!IsAdminRole() && currentUserId.HasValue && imtihon.OqituvchiId != currentUserId.Value)
                 {
                     return Json(new { success = false, message = "Bu imtihonga savol qo'shishga ruxsatingiz yo'q." });
                 }

                var savol = new ImtihonSavol
                {
                    ImtihonId = model.ImtihonId,
                    SavolMatni = model.SavolMatni,
                    VariantA = model.VariantA,
                    VariantB = model.VariantB,
                    VariantC = model.VariantC,
                    VariantD = model.VariantD,
                    TogriJavob = model.TogriJavob,
                    BallQiymati = model.BallQiymati
                };

                _context.ImtihonSavollar.Add(savol);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Savol muvaffaqiyatli qo'shildi.", savolId = savol.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Savol qo'shishda xatolik");
                return Json(new { success = false, message = "Xatolik: " + ex.Message });
            }
        }

        // 🗑️ Savol o'chirish
        [HttpPost]
        [Authorize(Roles = "teacher,admin")]
        public async Task<IActionResult> SavolOchirish([FromBody] IdRequest request)
        {
            try
            {
                if (!IsTeacherOrAdmin())
                {
                    return Json(new { success = false, message = "Ruxsat etilmagan amal." });
                }

                var currentUserId = GetCurrentUserId();
                if (GetCurrentRole() == "teacher" && !currentUserId.HasValue)
                {
                    return Json(new { success = false, message = "O'qituvchi ma'lumotlari topilmadi." });
                }

                if (request == null || request.Id <= 0)
                {
                    return Json(new { success = false, message = "Noto'g'ri so'rov." });
                }

                var savol = await _context.ImtihonSavollar
                    .Include(s => s.Imtihon)
                    .FirstOrDefaultAsync(s => s.Id == request.Id);
                if (savol == null)
                {
                    return Json(new { success = false, message = "Savol topilmadi." });
                }

                if (!IsAdminRole() && currentUserId.HasValue && savol.Imtihon?.OqituvchiId != currentUserId.Value)
                {
                    return Json(new { success = false, message = "Bu savolni o'chirish huquqingiz yo'q." });
                }

                _context.ImtihonSavollar.Remove(savol);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Savol o'chirildi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Savol o'chirishda xatolik");
                return Json(new { success = false, message = "Xatolik: " + ex.Message });
            }
        }

        // 📥 Savollarni fayldan import qilish
        [HttpPost]
        [Authorize(Roles = "teacher,admin")]
        public async Task<IActionResult> SavollarImport(IFormFile? file, int imtihonId, int? defaultBall)
        {
            try
            {
                if (!IsTeacherOrAdmin())
                {
                    return Json(new { success = false, message = "Ruxsat etilmagan amal." });
                }

                if (file == null || file.Length == 0)
                {
                    return Json(new { success = false, message = "Fayl tanlanmadi." });
                }

                var imtihon = await _context.Imtihonlar
                    .Include(i => i.Guruh)
                    .FirstOrDefaultAsync(i => i.Id == imtihonId);
                if (imtihon == null)
                {
                    return Json(new { success = false, message = "Imtihon topilmadi." });
                }

                var currentUserId = GetCurrentUserId();
                if (!IsAdminRole() && currentUserId.HasValue && imtihon.OqituvchiId != currentUserId.Value)
                {
                    return Json(new { success = false, message = "Bu imtihonga savol import qilish huquqingiz yo'q." });
                }

                var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(extension) || !(extension is ".pdf" or ".docx" or ".txt"))
                {
                    return Json(new { success = false, message = "Faqat .pdf, .docx yoki .txt formatlari qo'llab-quvvatlanadi." });
                }

                string content = extension switch
                {
                    ".pdf" => await ExtractTextFromPdfAsync(file),
                    ".docx" => await ExtractTextFromDocxAsync(file),
                    ".txt" => await ExtractTextFromPlainTextAsync(file),
                    _ => string.Empty
                };

                if (string.IsNullOrWhiteSpace(content))
                {
                    return Json(new { success = false, message = "Faylni o'qib bo'lmadi yoki fayl bo'sh." });
                }

                var fallbackBall = defaultBall.HasValue && defaultBall.Value > 0 ? defaultBall.Value : 1;
                var parsedQuestions = ParseBulkQuestions(content, fallbackBall, out var skippedReasons);

                if (!parsedQuestions.Any())
                {
                    var reason = skippedReasons.Any()
                        ? string.Join(" ", skippedReasons)
                        : "Hech qanday savol topilmadi.";
                    return Json(new { success = false, message = reason });
                }

                var letters = new[] { "A", "B", "C", "D" };
                var entities = parsedQuestions.Select(q =>
                {
                    var variants = q.Answers.Take(4).ToList();
                    var correctIndex = variants.FindIndex(v => v.IsCorrect);

                    return new ImtihonSavol
                    {
                        ImtihonId = imtihonId,
                        SavolMatni = q.Question,
                        VariantA = variants[0].Text,
                        VariantB = variants[1].Text,
                        VariantC = variants[2].Text,
                        VariantD = variants[3].Text,
                        TogriJavob = letters[correctIndex],
                        BallQiymati = q.Ball
                    };
                }).ToList();

                _context.ImtihonSavollar.AddRange(entities);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"{entities.Count} ta savol avtomatik qo'shildi.",
                    imported = entities.Count,
                    skipped = skippedReasons.Count,
                    errors = skippedReasons
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Savollarni import qilishda xatolik");
                return Json(new { success = false, message = "Faylni qayta ishlashda xatolik: " + ex.Message });
            }
        }

        // 👁️ Imtihonni ko'rish
        [HttpGet]
        public async Task<IActionResult> Tafsilot(int id)
        {
            try
            {
                var imtihon = await _context.Imtihonlar
                    .Include(i => i.Guruh)
                        .ThenInclude(g => g.Kurs)
                    .Include(i => i.Oqituvchi)
                    .Include(i => i.Savollar)
                    .Include(i => i.Natijalar)
                        .ThenInclude(n => n.Talaba)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (imtihon == null)
                {
                    TempData["ErrorMessage"] = "Imtihon topilmadi.";
                    return RedirectToAction("Index");
                }

                return View(imtihon);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Imtihon tafsilotlarini yuklashda xatolik");
                TempData["ErrorMessage"] = "Xatolik yuz berdi.";
                return RedirectToAction("Index");
            }
        }

        // ✏️ Imtihonni tahrirlash (GET)
        [HttpGet]
        [Authorize(Roles = "teacher,admin")]
        public async Task<IActionResult> Tahrirlash(int id)
        {
            try
            {
                if (!IsTeacherOrAdmin())
                {
                    return Forbid();
                }

                var currentUserId = GetCurrentUserId();
                var imtihon = await _context.Imtihonlar
                    .Include(i => i.Guruh)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (imtihon == null)
                {
                    TempData["ErrorMessage"] = "Imtihon topilmadi.";
                    return RedirectToAction("Index");
                }

                if (!IsAdminRole() && currentUserId.HasValue && imtihon.OqituvchiId != currentUserId.Value)
                {
                    return Forbid();
                }

                var model = new ImtihonCreateViewModel
                {
                    GuruhId = imtihon.GuruhId,
                    Nomi = imtihon.Nomi,
                    ImtihonTuri = imtihon.ImtihonTuri,
                    ImtihonFormati = imtihon.ImtihonFormati,
                    Sana = imtihon.Sana,
                    BoshlanishVaqti = imtihon.BoshlanishVaqti,
                    TugashVaqti = imtihon.TugashVaqti,
                    MuddatDaqiqada = imtihon.MuddatDaqiqada,
                    MinimalBall = imtihon.MinimalBall,
                    SertifikatBeriladimi = imtihon.SertifikatBeriladimi,
                    Izoh = imtihon.Izoh
                };

                await PopulateSelectListsAsync(model);
                ViewBag.ImtihonId = id;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Imtihon tahrirlash sahifasini yuklashda xatolik");
                TempData["ErrorMessage"] = "Xatolik yuz berdi.";
                return RedirectToAction("Index");
            }
        }

        // ✏️ Imtihonni tahrirlash (POST)
        [HttpPost]
        [Authorize(Roles = "teacher,admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Tahrirlash(int id, ImtihonCreateViewModel model)
        {
            try
            {
                if (!IsTeacherOrAdmin())
                {
                    return Forbid();
                }

                var currentUserId = GetCurrentUserId();
                if (!ModelState.IsValid)
                {
                    await PopulateSelectListsAsync(model);
                    ViewBag.ImtihonId = id;
                    return View(model);
                }

                var imtihon = await _context.Imtihonlar.FindAsync(id);
                if (imtihon == null)
                {
                    TempData["ErrorMessage"] = "Imtihon topilmadi.";
                    return RedirectToAction("Index");
                }

                if (!IsAdminRole() && currentUserId.HasValue && imtihon.OqituvchiId != currentUserId.Value)
                {
                    return Forbid();
                }

                imtihon.Nomi = model.Nomi;
                imtihon.ImtihonTuri = model.ImtihonTuri;
                imtihon.ImtihonFormati = model.ImtihonFormati;
                imtihon.Sana = model.Sana;
                imtihon.BoshlanishVaqti = model.BoshlanishVaqti;
                imtihon.TugashVaqti = model.TugashVaqti;
                imtihon.MuddatDaqiqada = model.MuddatDaqiqada;
                imtihon.MinimalBall = model.MinimalBall;
                imtihon.SertifikatBeriladimi = model.SertifikatBeriladimi;
                imtihon.Izoh = model.Izoh;
                imtihon.YangilanganVaqt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Imtihon muvaffaqiyatli yangilandi.";
                return RedirectToAction("Tafsilot", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Imtihon tahrirlashda xatolik");
                TempData["ErrorMessage"] = "Xatolik: " + ex.Message;
                await PopulateSelectListsAsync(model);
                ViewBag.ImtihonId = id;
                return View(model);
            }
        }

        // ▶️ Online imtihonni boshlash (davomatdan so'ng)
        [HttpPost]
        [Authorize(Roles = "teacher,admin")]
        public async Task<IActionResult> StartOnline([FromBody] IdRequest request)
        {
            try
            {
                if (!IsTeacherOrAdmin())
                {
                    return Json(new { success = false, message = "Ruxsat etilmagan." });
                }

                if (request == null || request.Id <= 0)
                {
                    return Json(new { success = false, message = "Noto‘g‘ri so‘rov." });
                }

                var currentUserId = GetCurrentUserId();
                var imtihon = await _context.Imtihonlar
                    .Include(i => i.Guruh)
                    .FirstOrDefaultAsync(i => i.Id == request.Id);

                if (imtihon == null)
                {
                    return Json(new { success = false, message = "Imtihon topilmadi." });
                }

                if (!IsAdminRole() && currentUserId.HasValue && imtihon.OqituvchiId != currentUserId.Value)
                {
                    return Json(new { success = false, message = "Bu imtihonni boshlashga huquqingiz yo'q." });
                }

                if (!string.Equals(imtihon.ImtihonFormati, "Online", StringComparison.OrdinalIgnoreCase))
                {
                    return Json(new { success = false, message = "Faqat online imtihonlar boshlanadi." });
                }

                var attendanceExists = await _context.Davomatlar
                    .AnyAsync(d => d.GuruhId == imtihon.GuruhId && d.Sana.Date == imtihon.Sana.Date);

                if (!attendanceExists)
                {
                    return Json(new { success = false, message = "Avval bugungi dars uchun davomat oling." });
                }

                // Faolligini yangilab qo'yamiz
                imtihon.Faolmi = true;
                imtihon.YangilanganVaqt = DateTime.Now;
                if (string.IsNullOrWhiteSpace(imtihon.BoshlanishVaqti))
                {
                    imtihon.BoshlanishVaqti = DateTime.Now.ToString("O");
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Online imtihon boshlandi. Talabalar ish boshlashi mumkin." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Online imtihonni boshlashda xatolik");
                return Json(new { success = false, message = "Xatolik: " + ex.Message });
            }
        }

        // 🗑️ Imtihonni o'chirish (AJAX POST)
        [HttpPost]
        [Authorize(Roles = "teacher,admin")]
        public async Task<IActionResult> Ochirish([FromBody] IdRequest request)
        {
            try
            {
                if (!IsTeacherOrAdmin())
                {
                    return Json(new { success = false, message = "Ruxsat etilmagan amal." });
                }

                if (request == null || request.Id <= 0)
                {
                    return Json(new { success = false, message = "Noto‘g‘ri so'rov." });
                }

                var currentUserId = GetCurrentUserId();
                var imtihon = await _context.Imtihonlar
                    .Include(i => i.Savollar)
                    .Include(i => i.Natijalar)
                    .FirstOrDefaultAsync(i => i.Id == request.Id);

                if (imtihon == null)
                {
                    return Json(new { success = false, message = "Imtihon topilmadi." });
                }

                if (!IsAdminRole() && currentUserId.HasValue && imtihon.OqituvchiId != currentUserId.Value)
                {
                    return Json(new { success = false, message = "Bu imtihonni o'chirish huquqingiz yo'q." });
                }

                // Agar natijalar bo'lsa, faqat o'chirib qo'yamiz
                if (imtihon.Natijalar != null && imtihon.Natijalar.Any())
                {
                    imtihon.Faolmi = false;
                    imtihon.YangilanganVaqt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Imtihon o'chirildi (faqat deaktivatsiya qilindi, chunki natijalar mavjud)." });
                }

                _context.Imtihonlar.Remove(imtihon);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Imtihon muvaffaqiyatli o'chirildi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Imtihon o'chirishda xatolik");
                return Json(new { success = false, message = "Xatolik: " + ex.Message });
            }
        }

        // 📊 Natijalar
        [HttpGet]
        [Authorize(Roles = "teacher,admin")]
        public async Task<IActionResult> Natijalar(int id)
        {
            try
            {
                if (!IsTeacherOrAdmin())
                {
                    return Forbid();
                }

                var currentUserId = GetCurrentUserId();
                var imtihon = await _context.Imtihonlar
                    .Include(i => i.Guruh)
                    .Include(i => i.Savollar)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (imtihon == null)
                {
                    TempData["ErrorMessage"] = "Imtihon topilmadi.";
                    return RedirectToAction("Index");
                }

                if (!IsAdminRole() && currentUserId.HasValue && imtihon.OqituvchiId != currentUserId.Value)
                {
                    return Forbid();
                }

                var natijalar = await _context.ImtihonNatijalar
                    .Include(n => n.Talaba)
                    .Where(n => n.ImtihonId == id)
                    .OrderByDescending(n => n.UmumiyBall)
                    .ToListAsync();

                ViewBag.Imtihon = imtihon;
                ViewBag.JamiSavollar = imtihon.Savollar?.Count ?? 0;
                ViewBag.JamiBall = imtihon.Savollar?.Sum(s => s.BallQiymati) ?? 0;
                ViewBag.NatijaManualStats = natijalar.ToDictionary(
                    n => n.Id,
                    n => ManualResultHelper.TryParse(n.JavoblarJson));

                return View(natijalar);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Natijalarni yuklashda xatolik");
                TempData["ErrorMessage"] = "Xatolik yuz berdi.";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        [Authorize(Roles = "teacher,admin")]
        public async Task<IActionResult> GetOfflineExams(int guruhId)
        {
            try
            {
                if (guruhId <= 0)
                {
                    return BadRequest(new { success = false, message = "Guruh tanlanmadi." });
                }

                var currentUserId = GetCurrentUserId();

                var examsQuery = _context.Imtihonlar
                    .Where(i => i.GuruhId == guruhId && i.ImtihonFormati == "Offline");

                if (!IsAdminRole() && currentUserId.HasValue)
                {
                    examsQuery = examsQuery.Where(i => i.OqituvchiId == currentUserId.Value);
                }

                var exams = await examsQuery
                    .OrderByDescending(i => i.Sana)
                    .Select(i => new
                    {
                        id = i.Id,
                        nomi = i.Nomi,
                        sana = i.Sana,
                        minimalBall = i.MinimalBall
                    })
                    .ToListAsync();

                return Json(new { success = true, exams });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Guruh bo'yicha imtihonlarni olishda xatolik");
                return Json(new { success = false, message = "Imtihonlar ro'yxatini yuklab bo'lmadi." });
            }
        }

        [HttpGet]
        [Authorize(Roles = "teacher,admin")]
        public async Task<IActionResult> LoadOfflineStudents(int imtihonId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var imtihon = await _context.Imtihonlar
                    .Include(i => i.Guruh!)
                        .ThenInclude(g => g.TalabaGuruhlar!)
                            .ThenInclude(tg => tg.Talaba)
                    .Include(i => i.Savollar)
                    .FirstOrDefaultAsync(i => i.Id == imtihonId);

                if (imtihon == null)
                {
                    return NotFound(new { success = false, message = "Imtihon topilmadi." });
                }

                if (!IsAdminRole() && currentUserId.HasValue && imtihon.OqituvchiId != currentUserId.Value)
                {
                    return Forbid();
                }

                if (!string.Equals(imtihon.ImtihonFormati, "Offline", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { success = false, message = "Faqat offline imtihonlar uchun natija kiritish mumkin." });
                }

                var talabalar = imtihon.Guruh?.TalabaGuruhlar?
                    .Where(tg => tg.Talaba != null)
                    .Select(tg => tg.Talaba!)
                    .OrderBy(t => t.Familiya)
                    .ThenBy(t => t.Ism)
                    .ToList() ?? new List<Foydalanuvchi>();

                if (!talabalar.Any())
                {
                    return Json(new { success = false, message = "Bu guruhga talabalar biriktirilmagan." });
                }

                var natijalar = await _context.ImtihonNatijalar
                    .Where(n => n.ImtihonId == imtihonId)
                    .ToDictionaryAsync(n => n.TalabaId, n => n);

                var manualTotal = natijalar.Values
                    .Select(n => ManualResultHelper.TryParse(n.JavoblarJson))
                    .FirstOrDefault(summary => summary?.TotalQuestions is not null);

                var maksimalBall = imtihon.Savollar?.Sum(s => s.BallQiymati)
                    ?? natijalar.Values.FirstOrDefault()?.MaksimalBall ?? 0;
                var jamiSavollar = imtihon.Savollar?.Count
                    ?? manualTotal?.TotalQuestions
                    ?? 0;

                var students = talabalar.Select(t =>
                {
                    natijalar.TryGetValue(t.Id, out var natija);
                    var manualSummary = ManualResultHelper.TryParse(natija?.JavoblarJson);
                    return new
                    {
                        talabaId = t.Id,
                        fio = $"{t.Familiya} {t.Ism}".Trim(),
                        telefon = t.TelefonRaqam,
                        ball = natija?.UmumiyBall,
                        togri = manualSummary?.CorrectAnswers,
                        foiz = natija?.FoizNatija,
                        status = natija == null ? null : (natija.Otdimi ? "O'tdi" : "O'tmadi")
                    };
                });

                return Json(new
                {
                    success = true,
                    exam = new
                    {
                        id = imtihon.Id,
                        nomi = imtihon.Nomi,
                        guruh = imtihon.Guruh?.Nomi,
                        sana = imtihon.Sana,
                        minimalBall = imtihon.MinimalBall,
                        maksimalBall,
                        jamiSavollar = jamiSavollar
                    },
                    students
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Offline natijalar uchun talabalarni yuklashda xatolik");
                return Json(new { success = false, message = "Talabalar ro'yxatini yuklab bo'lmadi." });
            }
        }

        [HttpPost]
        [Authorize(Roles = "teacher,admin")]
        public async Task<IActionResult> SaveOfflineResults([FromBody] OfflineResultSaveRequest request)
        {
            try
            {
                if (request == null || request.ImtihonId <= 0)
                {
                    return BadRequest(new { success = false, message = "Noto'g'ri so'rov." });
                }

                var currentUserId = GetCurrentUserId();
                var imtihon = await _context.Imtihonlar
                    .Include(i => i.Guruh!)
                        .ThenInclude(g => g.TalabaGuruhlar!)
                    .Include(i => i.Savollar)
                    .FirstOrDefaultAsync(i => i.Id == request.ImtihonId);

                if (imtihon == null)
                {
                    return NotFound(new { success = false, message = "Imtihon topilmadi." });
                }

                if (!IsAdminRole() && currentUserId.HasValue && imtihon.OqituvchiId != currentUserId.Value)
                {
                    return Forbid();
                }

                if (!string.Equals(imtihon.ImtihonFormati, "Offline", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { success = false, message = "Faqat offline imtihonlar uchun natija kiritish mumkin." });
                }

                var guruhTalabaIds = imtihon.Guruh?.TalabaGuruhlar?
                    .Select(tg => tg.TalabaId)
                    .ToHashSet() ?? new HashSet<int>();

                var entries = request.Students?
                    .Where(s => guruhTalabaIds.Contains(s.TalabaId) && s.Ball.HasValue)
                    .ToList() ?? new List<OfflineResultStudent>();

                if (!entries.Any())
                {
                    return BadRequest(new { success = false, message = "Hech bo'lmasa bitta talaba uchun ball kiriting." });
                }

                var maksimalBall = imtihon.Savollar?.Sum(s => s.BallQiymati) ?? 0;
                if (maksimalBall <= 0)
                {
                    maksimalBall = entries.Max(e => e.Ball) ?? 0;
                }
                if (maksimalBall <= 0)
                {
                    maksimalBall = 100;
                }

                var jamiSavollar = imtihon.Savollar?.Count;
                if ((jamiSavollar ?? 0) <= 0)
                {
                    jamiSavollar = entries.Max(e => e.TogriJavoblar) ?? 0;
                }

                var existingResults = await _context.ImtihonNatijalar
                    .Where(n => n.ImtihonId == request.ImtihonId)
                    .ToDictionaryAsync(n => n.TalabaId, n => n);

                var now = DateTime.Now;
                int updated = 0;
                int created = 0;

                foreach (var entry in entries)
                {
                    var ball = entry.Ball!.Value;
                    var foiz = maksimalBall > 0 ? (decimal)ball * 100 / maksimalBall : 0;
                    var otdimi = foiz >= imtihon.MinimalBall;
                    var manualPayload = ManualResultHelper.BuildPayload(ball, entry.TogriJavoblar, jamiSavollar);

                    if (existingResults.TryGetValue(entry.TalabaId, out var natija))
                    {
                        natija.UmumiyBall = ball;
                        natija.MaksimalBall = maksimalBall;
                        natija.FoizNatija = foiz;
                        natija.Otdimi = otdimi;
                        natija.JavoblarJson = manualPayload;
                        natija.Sana = now;
                        natija.YangilanganVaqt = now;
                        updated++;
                    }
                    else
                    {
                        natija = new ImtihonNatija
                        {
                            ImtihonId = request.ImtihonId,
                            TalabaId = entry.TalabaId,
                            UmumiyBall = ball,
                            MaksimalBall = maksimalBall,
                            FoizNatija = foiz,
                            Otdimi = otdimi,
                            Sana = now,
                            BoshlanishVaqti = imtihon.Sana,
                            TugashVaqti = imtihon.Sana,
                            JavoblarJson = manualPayload
                        };
                        _context.ImtihonNatijalar.Add(natija);
                        created++;
                    }

                    // Tangacha qo'shish (50% yuqori natijalar uchun)
                    if (foiz >= 50)
                    {
                        var talabaUser = await _context.Foydalanuvchilar.FindAsync(entry.TalabaId);
                        if (talabaUser != null)
                        {
                            // Imtihon uchun tangacha miqdori (masalan, 2000 so'm)
                            const decimal imtihonTangachaMiqdori = 2000m;
                            decimal qoshiladiganTangacha = imtihonTangachaMiqdori * foiz / 100;
                            
                            talabaUser.Tangacha += qoshiladiganTangacha;
                            talabaUser.YangilanganVaqt = now;
                        }
                    }

                    // Sertifikat yaratish: faqat online/offline imtihon sertifikatli bo'lsa va talaba o'tgan bo'lsa
                    if (imtihon.SertifikatBeriladimi && otdimi)
                    {
                        try
                        {
                            await SertifikatYaratishAsync(natija.Id, imtihon);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Offline natija uchun sertifikat yaratishda xatolik");
                        }
                    }
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Natijalar saqlandi. Yangi: {created}, yangilangan: {updated}",
                    created,
                    updated
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Offline natijalarni saqlashda xatolik");
                return Json(new { success = false, message = "Natijalarni saqlashda xatolik: " + ex.Message });
            }
        }

        // 📄 Imtihon natijasini PDF ko‘rinishida yuklab olish
        [HttpGet]
        [Authorize(Roles = "teacher,admin")]
        public async Task<IActionResult> NatijalarPdf(int id)
        {
            try
            {
                var imtihon = await _context.Imtihonlar
                    .Include(i => i.Guruh)
                        .ThenInclude(g => g.Kurs)
                    .Include(i => i.Oqituvchi)
                    .Include(i => i.Natijalar)
                        .ThenInclude(n => n.Talaba)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (imtihon == null)
                {
                    TempData["ErrorMessage"] = "Imtihon topilmadi.";
                    return RedirectToAction("Index");
                }

                using (var ms = new MemoryStream())
                {
                    var doc = new Document(PageSize.A4, 36, 36, 48, 48);
                    var writer = PdfWriter.GetInstance(doc, ms);
                    doc.Open();

                    var fontTitle = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                    var fontSubTitle = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                    var font = FontFactory.GetFont(FontFactory.HELVETICA, 10);

                    // Logo (agar mavjud bo'lsa)
                    try
                    {
                        var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "talimplus-logo.png");
                        if (System.IO.File.Exists(logoPath))
                        {
                            var logo = iTextSharp.text.Image.GetInstance(logoPath);
                            logo.ScaleToFit(80f, 80f);
                            logo.Alignment = Element.ALIGN_LEFT;
                            doc.Add(logo);
                        }
                    }
                    catch { }

                    // Markaziy sarlavha
                    var title = new Paragraph("TA'LIM PLUS EDUCATION CENTER", fontTitle)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 10f
                    };
                    doc.Add(title);

                    var sub = new Paragraph($"Imtihon natijalari — {imtihon.Nomi}", fontSubTitle)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 20f
                    };
                    doc.Add(sub);

                    // Asosiy ma'lumotlar
                    var infoTable = new PdfPTable(2) { WidthPercentage = 100 };
                    infoTable.SetWidths(new float[] { 30f, 70f });

                    void AddInfo(string left, string right)
                    {
                        infoTable.AddCell(new PdfPCell(new Phrase(left, font)) { Border = Rectangle.NO_BORDER });
                        infoTable.AddCell(new PdfPCell(new Phrase(right, font)) { Border = Rectangle.NO_BORDER });
                    }

                    AddInfo("Guruh:", imtihon.Guruh?.Nomi ?? "-");
                    AddInfo("Kurs:", imtihon.Guruh?.Kurs?.Nomi ?? "-");
                    AddInfo("O‘qituvchi:", $"{imtihon.Oqituvchi?.Familiya} {imtihon.Oqituvchi?.Ism}");
                    AddInfo("Sana:", imtihon.Sana.ToString("dd.MM.yyyy"));
                    AddInfo("Minimal ball (%):", imtihon.MinimalBall.ToString());

                    doc.Add(infoTable);
                    doc.Add(new Paragraph(" ", font));

                    // Natijalar jadvali
                    var table = new PdfPTable(6) { WidthPercentage = 100 };
                    table.SetWidths(new float[] { 8f, 30f, 15f, 15f, 12f, 20f });

                    void AddHeader(string text)
                    {
                        table.AddCell(new PdfPCell(new Phrase(text, fontSubTitle))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            BackgroundColor = new BaseColor(230, 230, 230)
                        });
                    }

                    AddHeader("#");
                    AddHeader("Talaba");
                    AddHeader("Ball");
                    AddHeader("Foiz");
                    AddHeader("Holat");
                    AddHeader("Sana");

                    int index = 1;
                    foreach (var n in imtihon.Natijalar.OrderByDescending(x => x.UmumiyBall))
                    {
                        table.AddCell(new PdfPCell(new Phrase(index.ToString(), font)));
                        table.AddCell(new PdfPCell(new Phrase($"{n.Talaba?.Familiya} {n.Talaba?.Ism}", font)));
                        table.AddCell(new PdfPCell(new Phrase($"{n.UmumiyBall} / {n.MaksimalBall}", font)));
                        table.AddCell(new PdfPCell(new Phrase($"{n.FoizNatija:F2} %", font)));
                        table.AddCell(new PdfPCell(new Phrase(n.Otdimi ? "O'tdi" : "O'tmadi", font)));
                        table.AddCell(new PdfPCell(new Phrase(n.Sana.ToString("dd.MM.yyyy HH:mm"), font)));
                        index++;
                    }

                    doc.Add(table);

                    // Footer
                    doc.Add(new Paragraph(" ", font));
                    var footer = new Paragraph(
                        $"Guruh: {imtihon.Guruh?.Nomi} | Kurs: {imtihon.Guruh?.Kurs?.Nomi} | Sana: {DateTime.Now:dd.MM.yyyy HH:mm}",
                        font)
                    {
                        Alignment = Element.ALIGN_RIGHT
                    };
                    doc.Add(footer);

                    doc.Close();

                    var bytes = ms.ToArray();
                    return File(bytes, "application/pdf", $"ImtihonNatijalari_{imtihon.Id}.pdf");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Imtihon natijalarini PDF shaklida yuklashda xatolik");
                TempData["ErrorMessage"] = "Xatolik: " + ex.Message;
                return RedirectToAction("Natijalar", new { id });
            }
        }

        // 🎓 Online imtihon berish (Talaba uchun)
        [HttpGet]
        [Authorize(Roles = "student")]
        public async Task<IActionResult> OnlineImtihon(int id)
        {
            try
            {
                if (!IsStudentRole())
                {
                    return Forbid();
                }

                var talabaId = GetCurrentUserId();
                if (!talabaId.HasValue)
                {
                    TempData["ErrorMessage"] = "Talaba ma'lumotlari topilmadi. Iltimos, qayta kirib keling.";
                    return RedirectToAction("Login", "Auth");
                }

                var imtihon = await _context.Imtihonlar
                    .Include(i => i.Guruh)
                    .Include(i => i.Savollar)
                    .FirstOrDefaultAsync(i => i.Id == id && i.Faolmi && i.ImtihonFormati == "Online");

                if (imtihon == null)
                {
                    TempData["ErrorMessage"] = "Imtihon topilmadi yoki online imtihon emas.";
                    return RedirectToAction("Index");
                }

                // Talaba bu guruhda ekanligini tekshirish
                var talabaGuruh = await _context.TalabaGuruhlar
                    .FirstOrDefaultAsync(tg => tg.TalabaId == talabaId.Value && tg.GuruhId == imtihon.GuruhId);

                if (talabaGuruh == null)
                {
                    TempData["ErrorMessage"] = "Siz bu imtihonni berish huquqiga ega emassiz.";
                    return RedirectToAction("Index");
                }

                var (eligible, reason) = await CheckAttendanceForOnlineExamAsync(talabaId.Value, imtihon);
                if (!eligible)
                {
                    TempData["ErrorMessage"] = reason;
                    return RedirectToAction("DashboardTalaba", "Oquvchi");
                }

                // Agar allaqachon berilgan bo'lsa
                var oldNatija = await _context.ImtihonNatijalar
                    .FirstOrDefaultAsync(n => n.ImtihonId == id && n.TalabaId == talabaId.Value);

                if (oldNatija != null)
                {
                    TempData["InfoMessage"] = "Siz bu imtihonni allaqachon berdingiz.";
                    return RedirectToAction("NatijaTafsilot", new { natijaId = oldNatija.Id });
                }

                ViewBag.Imtihon = imtihon;
                ViewBag.Muddat = imtihon.MuddatDaqiqada ?? 60;

                return View(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Online imtihon sahifasini yuklashda xatolik: {Message}", ex.Message);
                TempData["ErrorMessage"] = $"Xatolik yuz berdi: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // 📝 Online imtihon javoblarini saqlash
        [HttpPost]
        [Authorize(Roles = "student")]
        public async Task<IActionResult> OnlineImtihonJavob([FromBody] OnlineImtihonJavobViewModel model)
        {
            try
            {
                if (!IsStudentRole())
                {
                    return Json(new { success = false, message = "Faqat talabalar online imtihon topshirishi mumkin." });
                }

                var talabaId = GetCurrentUserId();
                if (!talabaId.HasValue)
                {
                    return Json(new { success = false, message = "Talaba ma'lumotlari topilmadi." });
                }

                var imtihon = await _context.Imtihonlar
                    .Include(i => i.Savollar)
                    .FirstOrDefaultAsync(i => i.Id == model.ImtihonId);

                if (imtihon == null)
                {
                    return Json(new { success = false, message = "Imtihon topilmadi." });
                }

                if (!imtihon.Faolmi || !string.Equals(imtihon.ImtihonFormati, "Online", StringComparison.OrdinalIgnoreCase))
                {
                    return Json(new { success = false, message = "Bu imtihon online rejimida emas." });
                }

                var talabaGuruh = await _context.TalabaGuruhlar
                    .FirstOrDefaultAsync(tg => tg.TalabaId == talabaId.Value && tg.GuruhId == imtihon.GuruhId);

                if (talabaGuruh == null)
                {
                    return Json(new { success = false, message = "Siz bu imtihonni topshirish huquqiga ega emassiz." });
                }

                var (eligible, reason) = await CheckAttendanceForOnlineExamAsync(talabaId.Value, imtihon);
                if (!eligible)
                {
                    return Json(new { success = false, message = reason });
                }

                var oldNatija = await _context.ImtihonNatijalar
                    .FirstOrDefaultAsync(n => n.ImtihonId == model.ImtihonId && n.TalabaId == talabaId.Value);

                if (oldNatija != null)
                {
                    return Json(new { success = false, message = "Siz bu imtihonni allaqachon topshirgansiz." });
                }

                model.Javoblar ??= new List<JavobItem>();

                // Javoblarni tekshirish va ball hisoblash
                int umumiyBall = 0;
                int maksimalBall = imtihon.Savollar?.Sum(s => s.BallQiymati) ?? 0;
                int togriSanog = 0;

                var javoblar = new List<object>();

                foreach (var javob in model.Javoblar)
                {
                    var savol = imtihon.Savollar?.FirstOrDefault(s => s.Id == javob.SavolId);
                    if (savol != null)
                    {
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
                }

                decimal foiz = maksimalBall > 0 ? (decimal)umumiyBall * 100 / maksimalBall : 0;
                bool otdimi = foiz >= imtihon.MinimalBall;

                var natija = new ImtihonNatija
                {
                    ImtihonId = model.ImtihonId,
                    TalabaId = talabaId.Value,
                    UmumiyBall = umumiyBall,
                    MaksimalBall = maksimalBall,
                    FoizNatija = foiz,
                    Otdimi = otdimi,
                    Sana = DateTime.Now,
                    BoshlanishVaqti = model.BoshlanishVaqti,
                    TugashVaqti = DateTime.Now,
                    JavoblarJson = JsonSerializer.Serialize(javoblar)
                };

                _context.ImtihonNatijalar.Add(natija);
                await _context.SaveChangesAsync();

                // Tangacha qo'shish (50% yuqori natijalar uchun)
                if (foiz >= 50)
                {
                    var talabaUser = await _context.Foydalanuvchilar.FindAsync(talabaId.Value);
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

                // Agar o'tgan bo'lsa va sertifikat beriladimi bo'lsa
                if (otdimi && imtihon.SertifikatBeriladimi)
                {
                    await SertifikatYaratishAsync(natija.Id, imtihon);
                }

                return Json(new
                {
                    success = true,
                    natijaId = natija.Id,
                    umumiyBall = umumiyBall,
                    maksimalBall = maksimalBall,
                    foiz = Math.Round(foiz, 2),
                    otdimi = otdimi
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Online imtihon javoblarini saqlashda xatolik");
                return Json(new { success = false, message = "Xatolik: " + ex.Message });
            }
        }

        // 📄 Natija tafsilotlari
        [HttpGet]
        public async Task<IActionResult> NatijaTafsilot(int natijaId)
        {
            try
            {
                var natija = await _context.ImtihonNatijalar
                    .Include(n => n.Imtihon)
                        .ThenInclude(i => i.Savollar)
                    .Include(n => n.Talaba)
                    .Include(n => n.Imtihon)
                        .ThenInclude(i => i.Oqituvchi)
                    .FirstOrDefaultAsync(n => n.Id == natijaId);

                if (natija == null)
                {
                    TempData["ErrorMessage"] = "Natija topilmadi.";
                    return RedirectToAction("Index");
                }

                var currentUserId = GetCurrentUserId();

                if (IsStudentRole() && currentUserId.HasValue && natija.TalabaId != currentUserId.Value)
                {
                    return Forbid();
                }

                if (IsTeacherOrAdmin()
                    && !IsAdminRole()
                    && currentUserId.HasValue
                    && natija.Imtihon?.OqituvchiId != currentUserId.Value)
                {
                    return Forbid();
                }

                // Javoblarni parse qilish
                List<object> javoblar = new();
                if (!string.IsNullOrEmpty(natija.JavoblarJson))
                {
                    try
                    {
                        javoblar = JsonSerializer.Deserialize<List<object>>(natija.JavoblarJson) ?? new();
                    }
                    catch { }
                }

                ViewBag.Javoblar = javoblar;

                return View(natija);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Natija tafsilotlarini yuklashda xatolik");
                TempData["ErrorMessage"] = "Xatolik yuz berdi.";
                return RedirectToAction("Index");
            }
        }

        // 🎓 Sertifikat berish
        [HttpPost]
        [Authorize(Roles = "teacher,admin")]
        public async Task<IActionResult> SertifikatBerish(int natijaId)
        {
            try
            {
                if (!IsTeacherOrAdmin())
                {
                    return Json(new { success = false, message = "Ruxsat etilmagan amal." });
                }

                var currentUserId = GetCurrentUserId();
                var natija = await _context.ImtihonNatijalar
                    .Include(n => n.Imtihon)
                        .ThenInclude(i => i.Guruh)
                            .ThenInclude(g => g.Kurs)
                    .Include(n => n.Talaba)
                    .FirstOrDefaultAsync(n => n.Id == natijaId);

                if (natija == null)
                {
                    return Json(new { success = false, message = "Natija topilmadi." });
                }

                if (!IsAdminRole() && currentUserId.HasValue && natija.Imtihon?.OqituvchiId != currentUserId.Value)
                {
                    return Json(new { success = false, message = "Sertifikat berish uchun huquqingiz yo'q." });
                }

                if (!natija.Otdimi)
                {
                    return Json(new { success = false, message = "Talaba imtihondan o'tmagan." });
                }

                // Agar allaqachon sertifikat berilgan bo'lsa
                var oldSertifikat = await _context.Sertifikatlar
                    .FirstOrDefaultAsync(s => s.ImtihonId == natija.ImtihonId && s.TalabaId == natija.TalabaId);

                if (oldSertifikat != null)
                {
                    return Json(new { success = false, message = "Sertifikat allaqachon berilgan." });
                }

                await SertifikatYaratishAsync(natijaId, natija.Imtihon);

                return Json(new { success = true, message = "Sertifikat muvaffaqiyatli yaratildi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sertifikat berishda xatolik");
                return Json(new { success = false, message = "Xatolik: " + ex.Message });
            }
        }

        // 📄 Imtihon savollarini PDF ko‘rinishida yuklab olish
        [HttpGet]
        [Authorize(Roles = "teacher,admin")]
        public async Task<IActionResult> ImtihonPdf(int id)
        {
            try
            {
                var imtihon = await _context.Imtihonlar
                    .Include(i => i.Guruh)
                        .ThenInclude(g => g.Kurs)
                    .Include(i => i.Oqituvchi)
                    .Include(i => i.Savollar)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (imtihon == null)
                {
                    TempData["ErrorMessage"] = "Imtihon topilmadi.";
                    return RedirectToAction("Index");
                }

                using (var ms = new MemoryStream())
                {
                    var doc = new Document(PageSize.A4, 36, 36, 48, 48);
                    var writer = PdfWriter.GetInstance(doc, ms);
                    doc.Open();

                    var fontTitle = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                    var fontSubTitle = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                    var font = FontFactory.GetFont(FontFactory.HELVETICA, 10);

                    // Logo (agar mavjud bo'lsa)
                    try
                    {
                        var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "talimplus-logo.png");
                        if (System.IO.File.Exists(logoPath))
                        {
                            var logo = iTextSharp.text.Image.GetInstance(logoPath);
                            logo.ScaleToFit(80f, 80f);
                            logo.Alignment = Element.ALIGN_LEFT;
                            doc.Add(logo);
                        }
                    }
                    catch { }

                    // Markaziy sarlavha
                    var title = new Paragraph("TA'LIM PLUS EDUCATION CENTER", fontTitle)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 8f
                    };
                    doc.Add(title);

                    var sub = new Paragraph($"Imtihon savollari — {imtihon.Nomi}", fontSubTitle)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 16f
                    };
                    doc.Add(sub);

                    // Asosiy ma'lumotlar
                    var infoTable = new PdfPTable(2) { WidthPercentage = 100 };
                    infoTable.SetWidths(new float[] { 30f, 70f });

                    void AddInfo(string left, string right)
                    {
                        infoTable.AddCell(new PdfPCell(new Phrase(left, font)) { Border = Rectangle.NO_BORDER });
                        infoTable.AddCell(new PdfPCell(new Phrase(right, font)) { Border = Rectangle.NO_BORDER });
                    }

                    AddInfo("Guruh:", imtihon.Guruh?.Nomi ?? "-");
                    AddInfo("Kurs:", imtihon.Guruh?.Kurs?.Nomi ?? "-");
                    AddInfo("O‘qituvchi:", $"{imtihon.Oqituvchi?.Familiya} {imtihon.Oqituvchi?.Ism}");
                    AddInfo("Sana:", imtihon.Sana.ToString("dd.MM.yyyy"));
                    AddInfo("Minimal ball (%):", imtihon.MinimalBall.ToString());

                    doc.Add(infoTable);
                    doc.Add(new Paragraph(" ", font));

                    // Savollar ro'yxati
                    int index = 1;
                    foreach (var s in imtihon.Savollar.OrderBy(x => x.Id))
                    {
                        doc.Add(new Paragraph($"{index}. {s.SavolMatni}", fontSubTitle));
                        doc.Add(new Paragraph($"   A) {s.VariantA}", font));
                        doc.Add(new Paragraph($"   B) {s.VariantB}", font));
                        doc.Add(new Paragraph($"   C) {s.VariantC}", font));
                        doc.Add(new Paragraph($"   D) {s.VariantD}", font));
                        doc.Add(new Paragraph(" ", font));
                        index++;
                    }

                    // Footer
                    doc.Add(new Paragraph(" ", font));
                    var footer = new Paragraph(
                        $"Guruh: {imtihon.Guruh?.Nomi} | Kurs: {imtihon.Guruh?.Kurs?.Nomi} | Sana: {DateTime.Now:dd.MM.yyyy HH:mm}",
                        font)
                    {
                        Alignment = Element.ALIGN_RIGHT
                    };
                    doc.Add(footer);

                    doc.Close();

                    var bytes = ms.ToArray();
                    return File(bytes, "application/pdf", $"Imtihon_{imtihon.Id}.pdf");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Imtihon savollarini PDF shaklida yuklashda xatolik");
                TempData["ErrorMessage"] = "Xatolik: " + ex.Message;
                return RedirectToAction("Tafsilot", new { id });
            }
        }

        // 🎓 Sertifikat yaratish (private metod)
        private async Task SertifikatYaratishAsync(int natijaId, Imtihon imtihon)
        {
            var natija = await _context.ImtihonNatijalar
                .Include(n => n.Talaba)
                .FirstOrDefaultAsync(n => n.Id == natijaId);

            if (natija == null || !natija.Otdimi) return;

            var guruh = await _context.Guruhlar
                .Include(g => g.Kurs)
                .FirstOrDefaultAsync(g => g.Id == imtihon.GuruhId);

            if (guruh?.Kurs == null) return;

            var sertifikatRaqami = $"CERT-{DateTime.Now:yyyyMMdd}-{natijaId:D6}";

            var sertifikat = new Sertifikat
            {
                TalabaId = natija.TalabaId,
                ImtihonId = imtihon.Id,
                KursId = guruh.KursId,
                SertifikatRaqami = sertifikatRaqami,
                SertifikatNomi = $"{guruh.Kurs.Nomi} - {imtihon.Nomi}",
                BerilganSana = DateTime.Now,
                YaroqlilikMuddati = DateTime.Now.AddYears(3),
                Ball = natija.UmumiyBall,
                Foiz = natija.FoizNatija,
                Status = "Faol"
            };

            _context.Sertifikatlar.Add(sertifikat);
            await _context.SaveChangesAsync();
        }

        // 📜 Sertifikatlar ro'yxati
        [HttpGet]
        public async Task<IActionResult> Sertifikatlar(int? talabaId)
        {
            try
            {
                var query = _context.Sertifikatlar
                    .Include(s => s.Talaba)
                    .Include(s => s.Imtihon)
                    .Include(s => s.Kurs)
                    .AsQueryable();

                var currentUserId = GetCurrentUserId();
                if (IsStudentRole())
                {
                    if (!currentUserId.HasValue)
                    {
                        TempData["ErrorMessage"] = "Talaba ma'lumotlari topilmadi.";
                        return RedirectToAction("Login", "Auth");
                    }
                    query = query.Where(s => s.TalabaId == currentUserId.Value);
                }
                else if (talabaId.HasValue)
                {
                    query = query.Where(s => s.TalabaId == talabaId.Value);
                }

                var sertifikatlar = await query
                    .OrderByDescending(s => s.BerilganSana)
                    .ToListAsync();

                return View(sertifikatlar);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sertifikatlarni yuklashda xatolik");
                TempData["ErrorMessage"] = "Xatolik yuz berdi.";
                return RedirectToAction("Index");
            }
        }

        // 📄 Sertifikat PDF yuklab olish
        [HttpGet]
        public async Task<IActionResult> SertifikatPdf(int id)
        {
            try
            {
                var sertifikat = await _context.Sertifikatlar
                    .Include(s => s.Talaba)
                    .Include(s => s.Kurs)
                    .Include(s => s.Imtihon)
                        .ThenInclude(i => i.Oqituvchi)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (sertifikat == null)
                {
                    TempData["ErrorMessage"] = "Sertifikat topilmadi.";
                    return RedirectToAction("Sertifikatlar");
                }

                var currentUserId = GetCurrentUserId();
                if (IsStudentRole() && currentUserId.HasValue && sertifikat.TalabaId != currentUserId.Value)
                {
                    return Forbid();
                }

                if (IsTeacherOrAdmin()
                    && !IsAdminRole()
                    && currentUserId.HasValue
                    && sertifikat.Imtihon?.OqituvchiId != currentUserId.Value)
                {
                    return Forbid();
                }

                using (var ms = new MemoryStream())
                {
                    var doc = new Document(PageSize.A4, 50, 50, 50, 50);
                    var writer = PdfWriter.GetInstance(doc, ms);
                    doc.Open();

                    var fontTitle = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 24);
                    var fontSubtitle = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                    var font = FontFactory.GetFont(FontFactory.HELVETICA, 12);

                    // Sarlavha
                    doc.Add(new Paragraph("SERTIFIKAT", fontTitle) { Alignment = Element.ALIGN_CENTER });
                    doc.Add(new Paragraph(" "));

                    // Matn
                    doc.Add(new Paragraph($"Ushbu sertifikat", font) { Alignment = Element.ALIGN_CENTER });
                    doc.Add(new Paragraph($"{sertifikat.Talaba.Familiya} {sertifikat.Talaba.Ism}", fontSubtitle) { Alignment = Element.ALIGN_CENTER });
                    doc.Add(new Paragraph($"ga", font) { Alignment = Element.ALIGN_CENTER });
                    doc.Add(new Paragraph($"{sertifikat.Kurs.Nomi} kursi bo'yicha", font) { Alignment = Element.ALIGN_CENTER });
                    doc.Add(new Paragraph($"{sertifikat.Imtihon.Nomi} imtihonidan muvaffaqiyatli o'tganligi uchun", font) { Alignment = Element.ALIGN_CENTER });
                    doc.Add(new Paragraph($"berildi.", font) { Alignment = Element.ALIGN_CENTER });
                    doc.Add(new Paragraph(" "));

                    // Ma'lumotlar
                    var tbl = new PdfPTable(2) { WidthPercentage = 100 };
                    tbl.SetWidths(new float[] { 40f, 60f });

                    void AddRow(string left, string right)
                    {
                        tbl.AddCell(new PdfPCell(new Phrase(left, font)) { Border = Rectangle.NO_BORDER });
                        tbl.AddCell(new PdfPCell(new Phrase(right, font)) { Border = Rectangle.NO_BORDER });
                    }

                    AddRow("Sertifikat raqami:", sertifikat.SertifikatRaqami);
                    AddRow("Ball:", $"{sertifikat.Ball} / {sertifikat.Foiz:F2}%");
                    AddRow("Berilgan sana:", sertifikat.BerilganSana.ToString("dd.MM.yyyy"));

                    doc.Add(tbl);

                    doc.Close();

                    var bytes = ms.ToArray();
                    return File(bytes, "application/pdf", $"Sertifikat_{sertifikat.SertifikatRaqami}.pdf");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sertifikat PDF yaratishda xatolik");
                TempData["ErrorMessage"] = "Xatolik: " + ex.Message;
                return RedirectToAction("Sertifikatlar");
            }
        }

        private async Task<(bool Eligible, string Message)> CheckAttendanceForOnlineExamAsync(int talabaId, Imtihon imtihon)
        {
            var davomat = await _context.Davomatlar
                .Where(d => d.TalabaId == talabaId &&
                            d.GuruhId == imtihon.GuruhId &&
                            d.Sana.Date == imtihon.Sana.Date)
                .OrderByDescending(d => d.YangilanganVaqt)
                .FirstOrDefaultAsync();

            if (davomat == null)
            {
                return (false, "Bugungi dars uchun davomat hali belgilanmagan.");
            }

            if (!YaroqliDavomatHolatlari.Contains(davomat.Holati ?? string.Empty))
            {
                return (false, $"Darsga kelgan talabalar imtihonni ishlashi mumkin. Sizning holatingiz: {davomat.Holati}");
            }

            return (true, string.Empty);
        }

        private List<ParsedQuestion> ParseBulkQuestions(string rawText, int fallbackBall, out List<string> reasons)
        {
            var result = new List<ParsedQuestion>();
            var parseReasons = new List<string>();

            if (string.IsNullOrWhiteSpace(rawText))
            {
                reasons = parseReasons;
                return result;
            }

            var normalized = rawText.Replace("\r\n", "\n").Replace('\r', '\n');
            var lines = normalized.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            string? currentQuestion = null;
            var answers = new List<ParsedAnswer>();
            var currentBall = fallbackBall;

            void ResetState()
            {
                currentQuestion = null;
                answers = new List<ParsedAnswer>();
                currentBall = fallbackBall;
            }

            void FinalizeQuestion()
            {
                if (string.IsNullOrWhiteSpace(currentQuestion))
                {
                    ResetState();
                    return;
                }

                if (answers.Count < 4)
                {
                    parseReasons.Add($"\"{currentQuestion}\" savoli uchun kamida 4 ta javob variantini belgilang.");
                    ResetState();
                    return;
                }

                var variants = answers.Take(4).ToList();
                var correctCount = variants.Count(a => a.IsCorrect);
                if (correctCount != 1)
                {
                    parseReasons.Add($"\"{currentQuestion}\" savolida aynan bitta to'g'ri javobni (+) bilan belgilang.");
                    ResetState();
                    return;
                }

                var sanitizedBall = currentBall > 0 ? currentBall : fallbackBall;
                result.Add(new ParsedQuestion(currentQuestion, variants, sanitizedBall));
                ResetState();
            }

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (line.StartsWith("?", StringComparison.Ordinal))
                {
                    FinalizeQuestion();
                    currentQuestion = line[1..].Trim();
                    if (string.IsNullOrWhiteSpace(currentQuestion))
                    {
                        parseReasons.Add("Savol matni ? belgidan so'ng bo'sh bo'lmasligi kerak.");
                        ResetState();
                    }
                    continue;
                }

                if (line.StartsWith("#ball", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("!ball", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("ball", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryParseBallLine(line, out var parsedBall) && parsedBall > 0)
                    {
                        currentBall = parsedBall;
                    }
                    continue;
                }

                if (line.StartsWith("+", StringComparison.Ordinal) || line.StartsWith("-", StringComparison.Ordinal))
                {
                    if (string.IsNullOrWhiteSpace(currentQuestion))
                    {
                        parseReasons.Add($"Javobdan oldin savol belgilanmadi: \"{line}\".");
                        continue;
                    }

                    var answerText = line[1..].Trim();
                    if (string.IsNullOrWhiteSpace(answerText))
                    {
                        parseReasons.Add($"\"{currentQuestion}\" savolida bo'sh javob topildi.");
                        continue;
                    }

                    answers.Add(new ParsedAnswer(answerText, line.StartsWith("+", StringComparison.Ordinal)));
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(currentQuestion) && !answers.Any())
                {
                    currentQuestion = $"{currentQuestion} {line}".Trim();
                }
                else if (answers.Any())
                {
                    var last = answers[^1];
                    answers[^1] = new ParsedAnswer($"{last.Text} {line}".Trim(), last.IsCorrect);
                }
            }

            FinalizeQuestion();
            reasons = parseReasons;
            return result;
        }

        private static bool TryParseBallLine(string line, out int value)
        {
            value = 0;
            var lowered = line.ToLowerInvariant();
            var index = lowered.IndexOf("ball", StringComparison.Ordinal);
            if (index < 0)
            {
                return false;
            }

            var numericPortion = lowered[(index + 4)..].TrimStart(':', '=', ' ', '\t');
            var separatorIndex = numericPortion.IndexOfAny(new[] { ' ', '\t' });
            if (separatorIndex >= 0)
            {
                numericPortion = numericPortion[..separatorIndex];
            }

            return int.TryParse(numericPortion, out value);
        }

        private static async Task<string> ExtractTextFromPdfAsync(IFormFile file)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;

            var builder = new StringBuilder();
            using (var document = UglyToad.PdfPig.PdfDocument.Open(ms, new ParsingOptions { ClipPaths = true }))
            {
                foreach (var page in document.GetPages())
                {
                    builder.AppendLine(page.Text);
                }
            }

            return builder.ToString();
        }

        private static async Task<string> ExtractTextFromDocxAsync(IFormFile file)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;

            var builder = new StringBuilder();
            using var document = WordprocessingDocument.Open(ms, false);
            var paragraphs = document.MainDocumentPart?.Document?.Descendants<WordParagraph>() ?? Enumerable.Empty<WordParagraph>();

            foreach (var paragraph in paragraphs)
            {
                var text = paragraph.InnerText;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    builder.AppendLine(text);
                }
            }

            return builder.ToString();
        }

        private static async Task<string> ExtractTextFromPlainTextAsync(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, true);
            return await reader.ReadToEndAsync();
        }

        // Helper metod
        private async Task PopulateSelectListsAsync(ImtihonCreateViewModel model)
        {
            var guruhQuery = _context.Guruhlar
                .Include(g => g.Kurs)
                .AsQueryable();

            var currentUserId = GetCurrentUserId();
            if (GetCurrentRole() == "teacher" && currentUserId.HasValue)
            {
                guruhQuery = guruhQuery.Where(g => g.OqituvchiId == currentUserId.Value);
            }

            var guruhlar = await guruhQuery.ToListAsync();

            model.Guruhlar = guruhlar.Select(g => new SelectListItem
            {
                Value = g.Id.ToString(),
                Text = $"{g.Nomi} ({g.Kurs?.Nomi})",
                Selected = g.Id == model.GuruhId
            }).ToList();

            model.ImtihonTurlari = new List<SelectListItem>
            {
                new SelectListItem { Value = "Haftalik", Text = "Haftalik" },
                new SelectListItem { Value = "Oylik", Text = "Oylik" },
                new SelectListItem { Value = "Yakuniy", Text = "Yakuniy" },
                new SelectListItem { Value = "Oraliq", Text = "Oraliq" }
            };

            model.ImtihonFormatlari = new List<SelectListItem>
            {
                new SelectListItem { Value = "Online", Text = "Online" },
                new SelectListItem { Value = "Offline", Text = "Offline" }
            };
        }

        private int? GetCurrentUserId()
        {
            if (_currentUserId.HasValue)
            {
                return _currentUserId;
            }

            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(claimId) && int.TryParse(claimId, out var parsed))
            {
                _currentUserId = parsed;
                return _currentUserId;
            }

            var sessionId = HttpContext.Session.GetString("FoydalanuvchiId");
            if (!string.IsNullOrEmpty(sessionId) && int.TryParse(sessionId, out parsed))
            {
                _currentUserId = parsed;
                return _currentUserId;
            }

            return null;
        }

        private string GetCurrentRole()
        {
            if (!string.IsNullOrWhiteSpace(_currentRole))
            {
                return _currentRole!;
            }

            var claimRole = User.FindFirstValue(ClaimTypes.Role);
            if (!string.IsNullOrWhiteSpace(claimRole))
            {
                _currentRole = RoleHelper.Normalize(claimRole);
                return _currentRole!;
            }

            var sessionRole = HttpContext.Session.GetString("Rol");
            _currentRole = RoleHelper.Normalize(sessionRole);
            return _currentRole!;
        }

        private bool IsAdminRole() => RoleHelper.IsAdmin(GetCurrentRole());
        private bool IsTeacherOrAdmin() => RoleHelper.IsTeacher(GetCurrentRole());
        private bool IsStudentRole() => RoleHelper.IsStudent(GetCurrentRole());

        private sealed record ParsedAnswer(string Text, bool IsCorrect);
        private sealed record ParsedQuestion(string Question, List<ParsedAnswer> Answers, int Ball);
    }

    public class IdRequest
    {
        public int Id { get; set; }
    }

    // ViewModel for online imtihon javob
    public class OnlineImtihonJavobViewModel
    {
        public int ImtihonId { get; set; }
        public DateTime? BoshlanishVaqti { get; set; }
        public List<JavobItem> Javoblar { get; set; } = new();
    }

    public class JavobItem
    {
        public int SavolId { get; set; }
        public string? TanlanganJavob { get; set; }
    }

    public class OfflineResultSaveRequest
    {
        public int ImtihonId { get; set; }
        public List<OfflineResultStudent> Students { get; set; } = new();
    }

    public class OfflineResultStudent
    {
        public int TalabaId { get; set; }
        public int? TogriJavoblar { get; set; }
        public int? Ball { get; set; }
    }
}

