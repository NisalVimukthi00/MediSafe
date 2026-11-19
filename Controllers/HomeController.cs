using ANDtr.Web.Data;
using ANDtr.Web.Models;
using ANDtr.Web.Services;
using MedSafe.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ANDtr.Web.Controllers
{
    /// <summary>Dashboard for both roles, plus shared error pages.</summary>
    public class HomeController : BaseController
    {
        public HomeController(ANDtrDbContext db, CurrentUserService current, SearchService search)
            : base(db, current, search) { }

        public IActionResult Index()
        {
            var guard = RequireSignIn();
            if (guard != null) return guard;

            ViewBag.Active = "dashboard";
            var me = Me!;

            if (me.IsCustomer) return CustomerDashboard(me);

            var myReviewIds = Db.RiskReviews.Where(r => r.OwnerId == me.Id).Select(r => r.Id).ToList();

            var reviews = Db.RiskReviews
                .Include(r => r.Owner)
                .Include(r => r.Notices)
                .Include(r => r.Shares)
                .Where(r => r.OwnerId == me.Id)
                .OrderByDescending(r => r.UpdatedAt)
                .Take(4)
                .ToList();

            ViewBag.TotalNotices = Db.SafetyNotices.Count();
            ViewBag.OpenNotices = Db.SafetyNotices.Count(n => n.Status != "Closed");
            ViewBag.CriticalNotices = Db.SafetyNotices.Count(n => n.Severity == "Critical");
            ViewBag.MyReviewCount = myReviewIds.Count;
            ViewBag.SharedReviewCount = Db.RiskReviews.Count(r => r.OwnerId == me.Id && r.Shares.Any());
            ViewBag.SelectionCount = Db.SelectedNotices.Count(s => s.UserId == me.Id);
            ViewBag.CommentTotal = Db.Comments.Count();
            ViewBag.MyCommentTotal = Db.Comments.Count(c => c.AuthorId == me.Id);

            ViewBag.RecentReviews = reviews;

            ViewBag.CriticalList = Db.SafetyNotices
                .Where(n => n.Severity == "Critical" && n.Status != "Closed")
                .OrderByDescending(n => n.NoticeDate)
                .Take(4)
                .ToList();

            ViewBag.RecentComments = Db.Comments
                .Include(c => c.Author)
                .Include(c => c.SafetyNotice)
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .ToList();

            return View();
        }

        private IActionResult CustomerDashboard(User me)
        {
            var shared = Db.RiskReviews
                .Include(r => r.Owner)
                .Include(r => r.Notices)
                .Include(r => r.Shares)
                .Where(r => r.Shares.Any(s => s.CustomerId == me.Id && !s.HiddenByCustomer))
                .OrderByDescending(r => r.UpdatedAt)
                .ToList();

            ViewBag.SharedReviews = shared;
            ViewBag.SharedNoticeTotal = shared.Sum(r => r.Notices.Count);
            ViewBag.MyCommentTotal = Db.Comments.Count(c => c.AuthorId == me.Id);

            return View("CustomerDashboard");
        }

        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult StatusCodePage(int code)
        {
            ViewBag.Code = code;
            ViewBag.Message = code == 404
                ? "The page you asked for does not exist in the ANDtr platform."
                : "That request could not be completed.";
            return View("Status");
        }

        public IActionResult Error()
        {
            ViewBag.Code = 500;
            ViewBag.Message = "Something went wrong while loading that page. Try again, or return to the dashboard.";
            return View("Status");
        }
    }
}
