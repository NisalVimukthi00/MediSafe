using MedSafe.Web.Data;
using MedSafe.Web.Models;
using MedSafe.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedSafe.Web.Controllers
{
    /// <summary>
    /// Customer portal: the findings shared with the signed-in customer. Notices and
    /// employee comments are read-only; the customer can add comments and remove a
    /// finding from their own list.
    /// </summary>
    public class SharedFindingsController : BaseController
    {
        public SharedFindingsController(MedSafeDbContext db, CurrentUserService current, SearchService search)
            : base(db, current, search) { }

        public IActionResult Index()
        {
            var guard = RequireCustomer();
            if (guard != null) return guard;

            ViewBag.Active = "shared";
            var me = Me!;

            var shared = Db.RiskReviews
                .Include(r => r.Owner)
                .Include(r => r.Notices).ThenInclude(rn => rn.SafetyNotice)
                .Include(r => r.Shares)
                .Where(r => r.Shares.Any(s => s.CustomerId == me.Id && !s.HiddenByCustomer))
                .OrderByDescending(r => r.UpdatedAt)
                .ToList();

            ViewBag.Comments = Db.Comments
                .Include(c => c.Author)
                .Where(c => shared.Select(r => r.Id).Contains(c.RiskReviewId ?? 0))
                .ToList();

            return View(shared);
        }

        /// <summary>Customer ends their own access to a finding (soft remove).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int reviewId)
        {
            var guard = RequireCustomer();
            if (guard != null) return guard;

            var share = Db.ReviewShares.FirstOrDefault(s =>
                s.RiskReviewId == reviewId && s.CustomerId == Me!.Id && !s.HiddenByCustomer);

            if (share != null)
            {
                share.HiddenByCustomer = true;
                Db.SaveChanges();
                Notify("That finding has been removed from your list.");
            }

            return RedirectToAction("Index");
        }
    }
}
