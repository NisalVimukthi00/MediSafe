using ANDtr.Web.Data;
using ANDtr.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ANDtr.Web.Controllers
{
    /// <summary>
    /// URL alias for the customer portal (/Shared). Keeps the navigation short while the
    /// real work is done by SharedFindingsController.
    /// </summary>
    public class SharedController : Controller
    {
        public IActionResult Index() => RedirectToAction("Index", "SharedFindings");
    }
}
