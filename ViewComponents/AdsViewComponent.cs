using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using talim_platforma.Data;

namespace talim_platforma.ViewComponents;

public class AdsViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;

    public AdsViewComponent(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync(string target = "all")
    {
        var normalizedTarget = (target ?? "all").Trim().ToLowerInvariant();

        var ads = await _context.Advertisements
            .Where(a => a.IsActive)
            .Select(a => new
            {
                Ad = a,
                Target = (a.Target ?? "all").Trim().ToLower()
            })
            .Where(x => x.Target == "all" || x.Target == normalizedTarget)
            .OrderByDescending(x => x.Ad.UpdatedAt)
            .Take(4)
            .Select(x => x.Ad)
            .ToListAsync();

        return View(ads);
    }
}

