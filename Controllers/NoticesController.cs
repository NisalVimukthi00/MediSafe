using MedSafe.Web.Data;
using MedSafe.Web.Models;
using MedSafe.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedSafe.Web.Controllers
{

    /// Search the notice dataset, open a notice, and the autocomplete endpoint used by
    /// the search box. Customers cannot reach any of these actions.
    public class NoticesController : BaseController
    {
        private const int PageSize = 8;

        public NoticesController(MedSafeDbContext db, CurrentUserService current, SearchService search)
            : base(db, current, search) { }

        public IActionResult Search(string? q, string? deviceType, string? manufacturer, string? severity,
            string? noticeType, DateTime? dateFrom, DateTime? dateTo, string? sort, int page = 1)
        {
            var guard = RequireEmployee();
            if (guard != null) return guard;

            ViewBag.Active = "search";

            var notices = Db.SafetyNotices.AsNoTracking().ToList();
            SearchEngine.Prepare(notices);

            var filter = new NoticeFilter
            {
                DeviceType = deviceType,
                Manufacturer = manufacturer,
                Severity = severity,
                NoticeType = noticeType,
                DateFrom = dateFrom,
                DateTo = dateTo
            };

            var sortOption = Enum.TryParse<NoticeSort>(sort, true, out var parsed) ? parsed : NoticeSort.Relevance;
            var outcome = SearchEngine.Run(notices, q, filter, sortOption);

            foreach (var s in Db.SelectedNotices.Where(s => s.UserId == Me!.Id).Select(s => s.SafetyNoticeId))
            {
                ViewBag.Selected ??= new HashSet<string>();
                ((HashSet<string>)ViewBag.Selected).Add(s);
            }
            ViewBag.Selected ??= new HashSet<string>();

            ViewBag.Page = Math.Max(1, page);
            ViewBag.PageSize = PageSize;

            ViewBag.DeviceTypes = notices.Select(n => n.DeviceType).Distinct().OrderBy(x => x).ToList();
            ViewBag.Manufacturers = notices.Select(n => n.Manufacturer).Distinct().OrderBy(x => x).ToList();
            ViewBag.NoticeTypes = notices.Select(n => n.NoticeType).Distinct().OrderBy(x => x).ToList();
            ViewBag.Severities = new[] { "Critical", "High", "Medium", "Low" };

            // keep every current parameter so sorting and paging do not lose the query
            ViewBag.QueryString = new Dictionary<string, string?>
            {
                ["q"] = q,
                ["deviceType"] = deviceType,
                ["manufacturer"] = manufacturer,
                ["severity"] = severity,
                ["noticeType"] = noticeType,
                ["dateFrom"] = dateFrom?.ToString("yyyy-MM-dd"),
                ["dateTo"] = dateTo?.ToString("yyyy-MM-dd")
            };

            return View(outcome);
        }

        [HttpGet]
        public IActionResult Suggest(string? q)
        {
            if (!IsEmployee) return Json(Array.Empty<object>());

            var notices = Db.SafetyNotices.AsNoTracking().ToList();
            SearchEngine.Prepare(notices);

            var items = SearchEngine.Suggest(notices, q)
                .Select(s => new { text = s.Text, kind = s.Kind })
                .ToList();

            return Json(items);
        }

        public IActionResult Details(string id, int? reviewId, string? from, string? q)
        {
            var guard = RequireSignIn();
            if (guard != null) return guard;

            var notice = Db.SafetyNotices.AsNoTracking().FirstOrDefault(n => n.Id == id);
            if (notice == null) return NotFound();

            RiskReview? review = null;

            if (reviewId.HasValue)
            {
                review = Db.RiskReviews.Include(r => r.Owner).Include(r => r.Notices)
                    .FirstOrDefault(r => r.Id == reviewId.Value);

                // a customer may only read a notice through a review shared with them
                if (review != null && !CanSeeReview(review)) review = null;
                if (review == null && IsCustomer) return View("~/Views/Shared/Restricted.cshtml", "shared");
            }

            if (IsCustomer && review == null) return View("~/Views/Shared/Restricted.cshtml", "shared");

            var comments = Db.Comments
                .Include(c => c.Author)
                .Where(c => c.SafetyNoticeId == id)
                .Where(c => reviewId == null ? c.RiskReviewId == null : c.RiskReviewId == reviewId)
                .OrderBy(c => c.CreatedAt)
                .ToList();

            ViewBag.Notice = notice;
            ViewBag.Review = review;
            ViewBag.From = from;
            ViewBag.Query = q;
            ViewBag.Comments = comments;

            ViewBag.Highlight = string.IsNullOrWhiteSpace(q) ? Array.Empty<string>() : Format.Tokenize(q);

            ViewBag.Selected = IsEmployee
                ? Db.SelectedNotices.Where(s => s.UserId == Me!.Id).Select(s => s.SafetyNoticeId).ToList()
                : new List<string>();

            ViewBag.Selection = IsEmployee
                ? Db.SelectedNotices.Include(s => s.SafetyNotice)
                    .Where(s => s.UserId == Me!.Id)
                    .OrderByDescending(s => s.AddedAt)
                    .Take(5)
                    .Select(s => s.SafetyNotice!)
                    .ToList()
                : new List<SafetyNotice>();

            ViewBag.MyReviews = IsEmployee
                ? Db.RiskReviews.Where(r => r.OwnerId == Me!.Id).OrderBy(r => r.Name).ToList()
                : new List<RiskReview>();

            return View();
        }

        /// <summary>Print-friendly single notice (browser handles "save as PDF").</summary>
        public IActionResult Print(string id)
        {
            var guard = RequireSignIn();
            if (guard != null) return guard;

            var notice = Db.SafetyNotices.AsNoTracking().FirstOrDefault(n => n.Id == id);
            if (notice == null) return NotFound();

            return View(notice);
        }
    }
}
