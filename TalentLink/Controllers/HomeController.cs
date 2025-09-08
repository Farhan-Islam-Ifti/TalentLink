using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TalentLink.Models;

namespace TalentLink.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // Public homepage (before login)
        public IActionResult Index()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                // Redirect logged-in users to their dashboard automatically
                return RedirectToAction("Dashboard");
            }

            return View();

        }

      public IActionResult About()
    {
        return View();
    }

    

        // Dashboard for logged-in users
        public IActionResult Dashboard()
        {
            return View(); // You’ll create Views/Home/Dashboard.cshtml
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
}
