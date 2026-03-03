using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Web.Controllers
{
    public class AboutUsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Developer()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }
    }
}