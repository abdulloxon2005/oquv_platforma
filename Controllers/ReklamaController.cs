using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using talim_platforma.Data;
using talim_platforma.Models;

namespace talim_platforma.Controllers;

[Authorize(Roles = "admin")]
[AutoValidateAntiforgeryToken]
public class ReklamaController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;

    public ReklamaController(ApplicationDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    public async Task<IActionResult> Index()
    {
        var ads = await _context.Advertisements
            .OrderByDescending(a => a.UpdatedAt)
            .ToListAsync();
        return View(ads);
    }

    public IActionResult Create()
    {
        return View(new Advertisement { IsActive = true, Target = "all" });
    }

    [HttpPost]
    public async Task<IActionResult> Create(Advertisement model, IFormFile? image)
    {
        if (image != null && image.Length > 0)
        {
            var saveResult = await SaveImageAsync(image);
            if (!saveResult.success)
            {
                ModelState.AddModelError(nameof(model.ImagePath), saveResult.errorMessage ?? "Rasmni saqlashda xato.");
            }
            else
            {
                model.ImagePath = saveResult.path;
            }
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        model.CreatedAt = DateTime.UtcNow;
        model.UpdatedAt = DateTime.UtcNow;
        _context.Advertisements.Add(model);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Reklama qo'shildi.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var ad = await _context.Advertisements.FindAsync(id);
        if (ad == null) return NotFound();
        return View(ad);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, Advertisement model, IFormFile? image)
    {
        var ad = await _context.Advertisements.FindAsync(id);
        if (ad == null) return NotFound();

        if (image != null && image.Length > 0)
        {
            var saveResult = await SaveImageAsync(image);
            if (!saveResult.success)
            {
                ModelState.AddModelError(nameof(model.ImagePath), saveResult.errorMessage ?? "Rasmni saqlashda xato.");
            }
            else
            {
                ad.ImagePath = saveResult.path;
            }
        }

        if (!ModelState.IsValid)
        {
            return View(ad);
        }

        ad.Title = model.Title;
        ad.Description = model.Description;
        ad.LinkUrl = model.LinkUrl;
        ad.Target = model.Target;
        ad.IsActive = model.IsActive;
        ad.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Reklama yangilandi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(int id)
    {
        var ad = await _context.Advertisements.FindAsync(id);
        if (ad == null) return NotFound();
        ad.IsActive = !ad.IsActive;
        ad.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var ad = await _context.Advertisements.FindAsync(id);
        if (ad == null) return NotFound();

        _context.Advertisements.Remove(ad);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Reklama o'chirildi.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<(bool success, string? path, string? errorMessage)> SaveImageAsync(IFormFile file)
    {
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
        {
            return (false, null, "Faqat jpg, jpeg, png, webp yuklash mumkin.");
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            return (false, null, "Fayl hajmi 5MB dan oshmasligi kerak.");
        }

        var uploadsRoot = Path.Combine(_env.WebRootPath ?? "wwwroot", "media", "ads");
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadsRoot, fileName);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativePath = $"/media/ads/{fileName}";
        return (true, relativePath, null);
    }
}

