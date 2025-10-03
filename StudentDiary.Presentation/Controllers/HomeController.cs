using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StudentDiary.Presentation.Models;
using StudentDiary.Presentation.Helpers;

namespace StudentDiary.Presentation.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        // Redirect authenticated users to their diary
        if (AuthenticationHelper.IsAuthenticated(HttpContext))
        {
            return RedirectToAction("Index", "Diary");
        }
        
        // Show landing page for non-authenticated users
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
