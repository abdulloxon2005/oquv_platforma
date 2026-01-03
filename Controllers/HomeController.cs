using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using talim_platforma.Models;
using talim_platforma.Data;

namespace talim_platforma.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        ViewData["ShowBanner"] = true;
        return View();
    }


    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet]
    public IActionResult KursgaYozilish()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> KursgaYozilish(KursApplication model)
    {
        if (ModelState.IsValid)
        {
            model.Sana = DateTime.Now;
            model.Faol = true;
            _context.KursApplications.Add(model);
            await _context.SaveChangesAsync();
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Arizangiz muvaffaqiyatli yuborildi!" });
            }
            
            TempData["Success"] = "Sizning arizangiz muvaffaqiyatli qabul qilindi! Tez orada siz bilan bog'lanamiz.";
            return RedirectToAction("Index");
        }
        
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new { success = false, errors = errors });
        }
        
        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
