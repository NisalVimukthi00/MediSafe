using System.Text.Json;
using MedSafe.Web.Data;
using MedSafe.Web.Models;
using MedSafe.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedSafe.Web.Controllers
{
    /// <summary>
    /// Reference screens: the user-story traceability matrix from the requirement document,
    /// and the data management page (counts, schema and database reset).
    /// </summary>
    public class ReferenceController : BaseController
    {
        private readonly IWebHostEnvironment _env;

        public ReferenceController(MedSafeDbContext db, CurrentUserService current, SearchService search,
            IWebHostEnvironment env) : base(db, current, search)
        {
            _env = env;
        }

        public IActionResult Coverage()
        {
            var guard = RequireSignIn();
            if (guard != null) return guard;

            ViewBag.Active = "coverage";

            var path = Path.Combine(_env.ContentRootPath, "Data", "Seed", "user-stories.json");

            var groups = new List<StoryGroup>();
            if (System.IO.File.Exists(path))
            {
                groups = JsonSerializer.Deserialize<List<StoryGroup>>(System.IO.File.ReadAllText(path),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            }

            ViewBag.NoticeCount = Db.SafetyNotices.Count();
            return View(groups);
        }

        public IActionResult Data()
        {
            var guard = RequireEmployee();
            if (guard != null) return guard;

            ViewBag.Active = "data";

            ViewBag.TotalNotices = Db.SafetyNotices.Count();
            ViewBag.TotalUsers = Db.Users.Count();
            ViewBag.CustomerCount = Db.Users.Count(u => u.Role == UserRole.Customer);
            ViewBag.ReviewCount = Db.RiskReviews.Count();
            ViewBag.CommentCount = Db.Comments.Count();
            ViewBag.SelectionCount = Db.SelectedNotices.Count(s => s.UserId == Me!.Id);

            ViewBag.SampleNotice = Db.SafetyNotices.AsNoTracking().OrderBy(n => n.Id).FirstOrDefault();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reset()
        {
            var guard = RequireEmployee();
            if (guard != null) return guard;

            // comments, reviews, shares and selections are cleared; accounts and notices stay
            Db.Comments.RemoveRange(Db.Comments);
            Db.ReviewShares.RemoveRange(Db.ReviewShares);
            Db.ReviewNotices.RemoveRange(Db.ReviewNotices);
            Db.RiskReviews.RemoveRange(Db.RiskReviews);
            Db.SelectedNotices.RemoveRange(Db.SelectedNotices);
            Db.SaveChanges();

            Notify("Risk reviews, comments and selections cleared. The notice dataset and accounts are unchanged.");
            return RedirectToAction("Data");
        }
    }

    /* ---------- shapes of the requirement document ---------- */

    public class StoryGroup
    {
        public string group { get; set; } = string.Empty;
        public string owner { get; set; } = string.Empty;
        public string sid { get; set; } = string.Empty;
        public List<StoryItem> items { get; set; } = new();
    }

    public class StoryItem
    {
        public int n { get; set; }
        public string title { get; set; } = string.Empty;
        public string story { get; set; } = string.Empty;
        public List<string> ac { get; set; } = new();
        public string where { get; set; } = string.Empty;
    }
}
