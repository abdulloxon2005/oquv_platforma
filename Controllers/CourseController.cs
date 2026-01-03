using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using talim_platforma.Data;
using talim_platforma.Models;

namespace talim_platforma.Controllers;

[Authorize(Roles = "admin")]
[AutoValidateAntiforgeryToken]
public class CourseController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;

    public CourseController(ApplicationDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    // GET: /Course
    public async Task<IActionResult> Index()
    {
        var courses = await _context.Courses
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        return View(courses);
    }

    // GET: /Course/Create
    public IActionResult Create()
    {
        return View(new Course { IsActive = true });
    }

    // POST: /Course/Create
    [HttpPost]
    public async Task<IActionResult> Create(Course model, IFormFile? image)
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
        _context.Courses.Add(model);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Kurs qo'shildi.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Course/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null) return NotFound();
        return View(course);
    }

    // POST: /Course/Edit/5
    [HttpPost]
    public async Task<IActionResult> Edit(int id, Course model, IFormFile? image)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null) return NotFound();

        if (image != null && image.Length > 0)
        {
            var saveResult = await SaveImageAsync(image);
            if (!saveResult.success)
            {
                ModelState.AddModelError(nameof(model.ImagePath), saveResult.errorMessage ?? "Rasmni saqlashda xato.");
            }
            else
            {
                course.ImagePath = saveResult.path;
            }
        }

        if (!ModelState.IsValid)
        {
            return View(course);
        }

        course.Title = model.Title;
        course.Description = model.Description;
        course.IsActive = model.IsActive;
        course.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Kurs yangilandi.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Course/Toggle/5
    [HttpPost]
    public async Task<IActionResult> Toggle(int id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null) return NotFound();
        
        course.IsActive = !course.IsActive;
        course.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
        return RedirectToAction(nameof(Index));
    }

    // POST: /Course/Delete/5
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null) return NotFound();

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Kurs o'chirildi.";
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

        var uploadsRoot = Path.Combine(_env.WebRootPath ?? "wwwroot", "media", "courses");
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadsRoot, fileName);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativePath = $"/media/courses/{fileName}";
        return (true, relativePath, null);
    }
}
