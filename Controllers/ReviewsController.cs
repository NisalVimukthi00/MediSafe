using MedSafe.Web.Data;
using MedSafe.Web.Models;
using MedSafe.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedSafe.Web.Controllers
{
    /// <summary>
    /// Risk reviews: create, rename, delete, add/remove notices, and share with customers.
    /// Every action is scoped to the signed-in employee's own reviews.
    /// </summary>
    public class ReviewsController : BaseController
    {
        public ReviewsController(MedSafeDbContext db, CurrentUserService current, SearchService search)
            : base(db, current, search) { }

        public IActionResult Index()
        {
            var guard = RequireEmployee();
            if (guard != null) return guard;

            ViewBag.Active = "reviews";

            var reviews = Db.RiskReviews
                .Include(r => r.Owner)
                .Include(r => r.Notices)
                .Include(r => r.Shares)
                .Where(r => r.OwnerId == Me!.Id)
                .OrderByDescending(r => r.UpdatedAt)
                .ToList();

            ViewBag.SelectionCount = Db.SelectedNotices.Count(s => s.UserId == Me!.Id);
            return View(reviews);
        }

        public IActionResult Details(int id)
        {
            var guard = RequireSignIn();
            if (guard != null) return guard;

            ViewBag.Active = IsEmployee ? "reviews" : "shared";

            var review = Db.RiskReviews
                .Include(r => r.Owner)
                .Include(r => r.Notices).ThenInclude(rn => rn.SafetyNotice)
                .Include(r => r.Shares).ThenInclude(s => s.Customer)
                .FirstOrDefault(r => r.Id == id);

            if (review == null) return NotFound();
            if (!CanSeeReview(review)) return View("~/Views/Shared/Restricted.cshtml", "shared");

            ViewBag.Notices = review.Notices
                .Select(rn => rn.SafetyNotice!)
                .Where(n => n != null)
                .OrderBy(n => n.NoticeDate)
                .ToList();

            ViewBag.Comments = Db.Comments
                .Include(c => c.Author)
                .Where(c => c.RiskReviewId == id)
                .OrderBy(c => c.CreatedAt)
                .ToList();

            ViewBag.Selection = IsEmployee
                ? Db.SelectedNotices.Include(s => s.SafetyNotice)
                    .Where(s => s.UserId == Me!.Id)
                    .Select(s => s.SafetyNotice!)
                    .ToList()
                : new List<SafetyNotice>();

            ViewBag.Customers = IsEmployee
                ? Db.Users.Where(u => u.Role == UserRole.Customer).OrderBy(u => u.Name).ToList()
                : new List<User>();

            ViewBag.ReturnUrl = Url.Action("Details", "Reviews", new { id });

            return View(review);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string name, string? description, bool useSelection)
        {
            var guard = RequireEmployee();
            if (guard != null) return guard;

            if (string.IsNullOrWhiteSpace(name))
            {
                Notify("Give the risk review a name.", "err");
                return RedirectToAction("Index");
            }

            var me = Me!;
            var review = new RiskReview
            {
                Name = name.Trim(),
                OwnerId = me.Id,
                Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            Db.RiskReviews.Add(review);
            Db.SaveChanges();

            if (useSelection)
            {
                var selection = Db.SelectedNotices.Include(s => s.SafetyNotice)
                    .Where(s => s.UserId == me.Id).ToList();

                foreach (var item in selection)
                {
                    Db.ReviewNotices.Add(new ReviewNotice
                    {
                        RiskReviewId = review.Id,
                        SafetyNoticeId = item.SafetyNoticeId,
                        AddedAt = DateTime.UtcNow
                    });
                }

                Db.SelectedNotices.RemoveRange(selection);
                Db.SaveChanges();

                Notify($"Risk review \"{review.Name}\" created with {selection.Count} notice(s).");
            }
            else
            {
                Notify($"Risk review \"{review.Name}\" created.");
            }

            return RedirectToAction("Details", new { id = review.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Rename(int id, string name, string? description)
        {
            var guard = RequireEmployee();
            if (guard != null) return guard;

            var review = OwnedReview(id);
            if (review == null) return Forbid();

            if (string.IsNullOrWhiteSpace(name))
            {
                Notify("The review name cannot be empty.", "err");
                return RedirectToAction("Details", new { id });
            }

            review.Name = name.Trim();
            review.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            review.UpdatedAt = DateTime.UtcNow;
            Db.SaveChanges();

            Notify("Risk review updated.");
            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var guard = RequireEmployee();
            if (guard != null) return guard;

            var review = OwnedReview(id);
            if (review == null) return Forbid();

            Db.RiskReviews.Remove(review);
            Db.SaveChanges();

            Notify($"Risk review \"{review.Name}\" deleted.");
            return RedirectToAction("Index");
        }

        /// <summary>Adds one notice to a review, never the same notice twice.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddNotice(int id, string noticeId, string? returnUrl)
        {
            var guard = RequireEmployee();
            if (guard != null) return guard;

            var review = OwnedReview(id);
            if (review == null) return Forbid();

            if (!Db.SafetyNotices.Any(n => n.Id == noticeId))
            {
                Notify("That notice could not be found.", "err");
                return RedirectToAction("Details", new { id });
            }

            if (Db.ReviewNotices.Any(rn => rn.RiskReviewId == id && rn.SafetyNoticeId == noticeId))
            {
                Notify("That notice is already in this review.", "warn");
            }
            else
            {
                Db.ReviewNotices.Add(new ReviewNotice
                {
                    RiskReviewId = id,
                    SafetyNoticeId = noticeId,
                    AddedAt = DateTime.UtcNow
                });

                review.UpdatedAt = DateTime.UtcNow;
                Db.SaveChanges();

                Notify("Notice added to the review.");
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Details", new { id });
        }

        /// <summary>Moves every notice in the selection bucket into a review.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddSelection(int id)
        {
            var guard = RequireEmployee();
            if (guard != null) return guard;

            var review = OwnedReview(id);
            if (review == null) return Forbid();

            var me = Me!;
            var selection = Db.SelectedNotices.Where(s => s.UserId == me.Id).ToList();
            var existing = Db.ReviewNotices.Where(rn => rn.RiskReviewId == id)
                .Select(rn => rn.SafetyNoticeId).ToHashSet();

            var added = 0;

            foreach (var item in selection)
            {
                if (existing.Contains(item.SafetyNoticeId)) continue;

                Db.ReviewNotices.Add(new ReviewNotice
                {
                    RiskReviewId = id,
                    SafetyNoticeId = item.SafetyNoticeId,
                    AddedAt = DateTime.UtcNow
                });
                added++;
            }

            Db.SelectedNotices.RemoveRange(selection);
            review.UpdatedAt = DateTime.UtcNow;
            Db.SaveChanges();

            Notify(added == 0
                ? "Those notices were already in the review."
                : $"Added {added} notice(s) to the review.", added == 0 ? "warn" : "ok");

            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveNotice(int id, string noticeId)
        {
            var guard = RequireEmployee();
            if (guard != null) return guard;

            var review = OwnedReview(id);
            if (review == null) return Forbid();

            var link = Db.ReviewNotices.FirstOrDefault(rn => rn.RiskReviewId == id && rn.SafetyNoticeId == noticeId);
            if (link != null)
            {
                Db.ReviewNotices.Remove(link);
                review.UpdatedAt = DateTime.UtcNow;
                Db.SaveChanges();
                Notify("Notice removed from the review.");
            }

            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Share(int id, int customerId)
        {
            var guard = RequireEmployee();
            if (guard != null) return guard;

            var review = OwnedReview(id);
            if (review == null) return Forbid();

            var customer = Db.Users.FirstOrDefault(u => u.Id == customerId && u.Role == UserRole.Customer);
            if (customer == null)
            {
                Notify("That customer account could not be found.", "err");
                return RedirectToAction("Details", new { id });
            }

            var existing = Db.ReviewShares.FirstOrDefault(s => s.RiskReviewId == id && s.CustomerId == customerId);

            if (existing != null)
            {
                if (existing.HiddenByCustomer)
                {
                    existing.HiddenByCustomer = false;
                    existing.SharedAt = DateTime.UtcNow;
                    existing.SharedBy = Me!.Name;
                    Db.SaveChanges();
                    Notify($"Sharing with {customer.Name} restored.");
                }
                else
                {
                    Notify($"This review is already shared with {customer.Name}.", "warn");
                }
            }
            else
            {
                Db.ReviewShares.Add(new ReviewShare
                {
                    RiskReviewId = id,
                    CustomerId = customerId,
                    SharedAt = DateTime.UtcNow,
                    SharedBy = Me!.Name
                });

                review.UpdatedAt = DateTime.UtcNow;
                Db.SaveChanges();

                Notify($"Shared \"{review.Name}\" with {customer.Name}.");
            }

            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Unshare(int id, int customerId)
        {
            var guard = RequireEmployee();
            if (guard != null) return guard;

            var review = OwnedReview(id);
            if (review == null) return Forbid();

            var share = Db.ReviewShares.FirstOrDefault(s => s.RiskReviewId == id && s.CustomerId == customerId);
            if (share != null)
            {
                var name = Db.Users.Where(u => u.Id == customerId).Select(u => u.Name).FirstOrDefault();
                Db.ReviewShares.Remove(share);
                review.UpdatedAt = DateTime.UtcNow;
                Db.SaveChanges();
                Notify($"Stopped sharing with {name ?? "that customer"}.");
            }

            return RedirectToAction("Details", new { id });
        }

        /// <summary>Loads a review only when it belongs to the signed-in employee.</summary>
        private RiskReview? OwnedReview(int id)
        {
            var me = Me;
            if (me == null || !me.IsEmployee) return null;

            return Db.RiskReviews.FirstOrDefault(r => r.Id == id && r.OwnerId == me.Id);
        }
    }
}
