using Microsoft.AspNetCore.Mvc;

namespace LogReaderBackend.Controllers
{
    public class LogReader : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
