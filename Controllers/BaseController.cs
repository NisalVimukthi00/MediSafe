using ANDtr.Web.Data;
using ANDtr.Web.Models;
using ANDtr.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace ANDtr.Web.Controllers
{
    /// <summary>
    /// Shared base for every controller: loads the signed-in account and the counter
    /// badges used by the sidebar, and enforces the employee / customer split.
    /// </summary>
    public abstract class BaseController : Controller
    {
        protected readonly ANDtrDbContext Db;
        protected readonly CurrentUserService Current;
        protected readonly SearchService SearchEngine;

        protected BaseController(ANDtrDbContext db, CurrentUserService current, SearchService search)
        {
            Db = db;
            Current = current;
            SearchEngine = search;
        }

        protected User? Me => Current.User;
        protected bool IsEmployee => Current.IsEmployee;
        protected bool IsCustomer => Current.IsCustomer;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            var user = Current.User;
            ViewBag.CurrentUser = user;

            if (user != null)
            {
                ViewBag.NoticeCount = Db.SafetyNotices.Count();
                ViewBag.CommentCount = Db.Comments.Count();
                ViewBag.SelectionCount = Db.SelectedNotices.Count(s => s.UserId == user.Id);
                ViewBag.MyCommentCount = Db.Comments.Count(c => c.AuthorId == user.Id);

                ViewBag.ReviewCount = user.IsEmployee
                    ? Db.RiskReviews.Count(r => r.OwnerId == user.Id)
                    : Db.ReviewShares.Count(s => s.CustomerId == user.Id && !s.HiddenByCustomer);

                ViewBag.CustomerCount = user.IsEmployee
                    ? Db.Users.Count(u => u.Role == UserRole.Customer)
                    : 0;
            }

            ViewData["GlobalQuery"] = HttpContext.Request.Query["q"].ToString();
        }

        /// <summary>Redirects to the sign-in screen when there is no session.</summary>
        protected IActionResult? RequireSignIn()
        {
            return Me == null ? RedirectToAction("Login", "Account", new { returnUrl = Request.Path + Request.QueryString }) : null;
        }

        /// <summary>Blocks customer accounts from employee-only functions.</summary>
        protected IActionResult? RequireEmployee()
        {
            if (Me == null) return RedirectToAction("Login", "Account");
            return IsEmployee ? null : View("~/Views/Shared/Restricted.cshtml", "employee");
        }

        /// <summary>Blocks employees from the customer portal (they use their own views).</summary>
        protected IActionResult? RequireCustomer()
        {
            if (Me == null) return RedirectToAction("Login", "Account");
            return IsCustomer ? null : View("~/Views/Shared/Restricted.cshtml", "customer");
        }

        /// <summary>
        /// Confirms the current user may open a review: employees only see their own,
        /// customers only see reviews shared with their own account.
        /// </summary>
        protected bool CanSeeReview(RiskReview review)
        {
            var me = Me;
            if (me == null) return false;
            if (me.IsEmployee) return review.OwnerId == me.Id;

            return Db.ReviewShares.Any(s =>
                s.RiskReviewId == review.Id && s.CustomerId == me.Id && !s.HiddenByCustomer);
        }

        /// <summary>True when the review has been shared with the signed-in customer.</summary>
        protected bool SharedWithMe(RiskReview review)
        {
            var me = Me;
            if (me == null || !me.IsCustomer) return false;

            return Db.ReviewShares.Any(s =>
                s.RiskReviewId == review.Id && s.CustomerId == me.Id && !s.HiddenByCustomer);
        }

        protected void Notify(string message, string kind = "ok") => TempData[kind] = message;
    }
}
