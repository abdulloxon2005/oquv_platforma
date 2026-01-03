using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using talim_platforma.Data;

namespace talim_platforma.ViewComponents;

public class CoursesViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;

    public CoursesViewComponent(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var courses = await _context.Courses
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return View(courses);
    }
}
