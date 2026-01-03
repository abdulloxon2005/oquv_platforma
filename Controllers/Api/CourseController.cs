using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using talim_platforma.Data;
using talim_platforma.Models;

namespace talim_platforma.Controllers.Api;

[Route("api/[controller]")]
[ApiController]
public class CourseController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher<Foydalanuvchi> _passwordHasher;

    public CourseController(ApplicationDbContext context)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<Foydalanuvchi>();
    }

    // GET: api/course - Faol kurslarni olish
    [HttpGet]
    public async Task<IActionResult> GetActiveCourses()
    {
        var courses = await _context.Courses
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.Description,
                c.ImagePath,
                c.CreatedAt
            })
            .ToListAsync();

        return Ok(new { success = true, data = courses });
    }

    // POST: api/course/enroll - Kursga ro'yxatdan o'tish
    [HttpPost("enroll")]
    public async Task<IActionResult> EnrollCourse([FromBody] EnrollCourseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || 
            string.IsNullOrWhiteSpace(request.LastName) || 
            string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return BadRequest(new { success = false, message = "Barcha maydonlarni to'ldiring." });
        }

        // Telefon raqam formatini tekshirish
        if (!request.PhoneNumber.StartsWith("+998") || request.PhoneNumber.Length != 13)
        {
            return BadRequest(new { success = false, message = "Telefon raqam +998 bilan boshlanishi va 13 ta raqamdan iborat bo'lishi kerak." });
        }

        // Kurs mavjudligini tekshirish - default kurs ID 1
        int courseId = 1; // Default kurs ID
        if (request.CourseId > 0)
        {
            courseId = request.CourseId;
        }

        var course = await _context.Courses.FindAsync(courseId);
        if (course == null || !course.IsActive)
        {
            // Agar kurs topilmasa, boshqa kurslarni qidirish
            course = await _context.Courses
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync();
            
            if (course == null)
            {
                return BadRequest(new { success = false, message = "Faol kurslar topilmadi." });
            }
            courseId = course.Id;
        }

        // Yangi foydalanuvchi yaratish
        var existingUser = await _context.Foydalanuvchilar
            .FirstOrDefaultAsync(f => f.TelefonRaqam == request.PhoneNumber);

        Foydalanuvchi foydalanuvchi;
        
        if (existingUser == null)
        {
            // Yangi foydalanuvchi yaratish
            foydalanuvchi = new Foydalanuvchi
            {
                Ism = request.FirstName.Trim(),
                Familiya = request.LastName.Trim(),
                TelefonRaqam = request.PhoneNumber.Trim(),
                Login = request.PhoneNumber.Trim(),
                Parol = _passwordHasher.HashPassword(null, "123456"), // Default parol
                Rol = "student",
                YaratilganVaqt = DateTime.Now,
                YangilanganVaqt = DateTime.Now
            };
            
            _context.Foydalanuvchilar.Add(foydalanuvchi);
            await _context.SaveChangesAsync();
        }
        else
        {
            foydalanuvchi = existingUser;
        }

        // Avval ro'yxatdan o'tganligini tekshirish
        var existingEnrollment = await _context.CourseEnrollments
            .AnyAsync(ce => ce.CourseId == courseId && ce.TalabaId == foydalanuvchi.Id && ce.IsActive);

        if (existingEnrollment)
        {
            return BadRequest(new { success = false, message = "Siz ushbu kursga avval ro'yxatdan o'tgansiz." });
        }

        // Ro'yxatdan o'tish
        var enrollment = new CourseEnrollment
        {
            CourseId = courseId,
            TalabaId = foydalanuvchi.Id,
            EnrolledAt = DateTime.UtcNow,
            IsActive = true
        };

        try
        {
            _context.CourseEnrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            return Ok(new { 
                success = true, 
                message = "Kursga muvaffaqiyatli ro'yxatdan o'tdingiz!",
                data = new
                {
                    enrollmentId = enrollment.Id,
                    courseName = course.Title,
                    studentName = $"{foydalanuvchi.Ism} {foydalanuvchi.Familiya}"
                }
            });
        }
        catch (Exception ex)
        {
            // Log error (optional)
            return BadRequest(new { 
                success = false, 
                message = $"Ro'yxatdan o'tishda xatolik: {ex.Message}" 
            });
        }
    }

    // GET: api/course/my-courses - Mening kurslarim
    [HttpGet("my-courses")]
    [Authorize(Roles = "student")]
    public async Task<IActionResult> GetMyCourses()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { success = false, message = "Avtorizatsiya xatosi." });
        }

        var talabaId = int.Parse(userId);

        var myCourses = await _context.CourseEnrollments
            .Where(ce => ce.TalabaId == talabaId && ce.IsActive)
            .Include(ce => ce.Course)
            .Select(ce => new
            {
                ce.Id,
                CourseId = ce.Course.Id,
                CourseTitle = ce.Course.Title,
                CourseDescription = ce.Course.Description,
                CourseImagePath = ce.Course.ImagePath,
                ce.EnrolledAt,
                courseStatus = ce.Course.IsActive ? "Faol" : "Faol emas"
            })
            .OrderByDescending(ce => ce.EnrolledAt)
            .ToListAsync();

        return Ok(new { success = true, data = myCourses });
    }
}

public class EnrollCourseRequest
{
    public int CourseId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}
