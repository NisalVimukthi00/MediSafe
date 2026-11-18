using MedSafe.Web.Data;
using MedSafe.Web.Models;
using MedSafe.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedSafe.Web.Controllers
{
    /// <summary>
    /// Comment add / edit / delete. Authors can only change their own comments; customers
    /// can only comment on reviews shared with them, and every comment is labelled with
    /// the author role so employee analysis and customer feedback never mix.
    /// </summary>
    public class CommentsController : BaseController
    {
        public CommentsController(MedSafeDbContext db, CurrentUserService current, SearchService search)
            : base(db, current, search) { }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string noticeId, int? reviewId, string? text, string? returnUrl)
        {
            var guard = RequireSignIn();
            if (guard != null) return guard;

            var me = Me!;

            if (string.IsNullOrWhiteSpace(text))
            {
                Notify("Write something before saving the comment.", "err");
                return Back(returnUrl, noticeId, reviewId);
            }

            if (!Db.SafetyNotices.Any(n => n.Id == noticeId))
            {
                Notify("That notice could not be found.", "err");
                return RedirectToAction("Index", "Home");
            }

            if (reviewId.HasValue)
            {
                var review = Db.RiskReviews.FirstOrDefault(r => r.Id == reviewId.Value);

                // customers may only comment inside a review that was shared with them
                if (review == null || (me.IsCustomer && !SharedWithMe(review)))
                {
                    Notify("You can only comment on findings that have been shared with you.", "err");
                    return RedirectToAction("Index", "Home");
                }

                if (me.IsEmployee && review.OwnerId != me.Id && !Db.Users.Any(u => u.Id == review.OwnerId))
                {
                    return Forbid();
                }
            }
            else if (me.IsCustomer)
            {
                Notify("Open a shared finding to add a comment.", "err");
                return RedirectToAction("Index", "Shared");
            }

            Db.Comments.Add(new Comment
            {
                SafetyNoticeId = noticeId,
                RiskReviewId = reviewId,
                AuthorId = me.Id,
                AuthorRole = me.Role,
                Text = text.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            if (reviewId.HasValue)
            {
                var review = Db.RiskReviews.FirstOrDefault(r => r.Id == reviewId.Value);
                if (review != null) review.UpdatedAt = DateTime.UtcNow;
            }

            Db.SaveChanges();

            Notify(me.IsCustomer
                ? "Your comment has been added — the company can see it."
                : "Comment saved to the database.");

            return Back(returnUrl, noticeId, reviewId);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, string? text, string? returnUrl)
        {
            var guard = RequireSignIn();
            if (guard != null) return guard;

            var comment = Db.Comments.FirstOrDefault(c => c.Id == id);
            if (comment == null) return NotFound();

            if (comment.AuthorId != Me!.Id)
            {
                Notify("You can only edit your own comments.", "err");
                return Redirect(returnUrl ?? "/");
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                Notify("A comment cannot be empty.", "err");
                return Redirect(returnUrl ?? "/");
            }

            comment.Text = text.Trim();
            comment.UpdatedAt = DateTime.UtcNow;
            Db.SaveChanges();

            Notify("Comment updated.");
            return Redirect(returnUrl ?? "/");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id, string? returnUrl)
        {
            var guard = RequireSignIn();
            if (guard != null) return guard;

            var comment = Db.Comments.FirstOrDefault(c => c.Id == id);
            if (comment == null) return NotFound();

            if (comment.AuthorId != Me!.Id)
            {
                Notify("You can only delete your own comments.", "err");
                return Redirect(returnUrl ?? "/");
            }

            Db.Comments.Remove(comment);
            Db.SaveChanges();

            Notify("Comment deleted.");
            return Redirect(returnUrl ?? "/");
        }

        /// <summary>Customer view: every comment the signed-in customer has written.</summary>
        public IActionResult Mine()
        {
            var guard = RequireCustomer();
            if (guard != null) return guard;

            ViewBag.Active = "mycomments";

            var mine = Db.Comments
                .Include(c => c.SafetyNotice)
                .Include(c => c.RiskReview)
                .Where(c => c.AuthorId == Me!.Id)
                .OrderByDescending(c => c.CreatedAt)
                .ToList();

            return View(mine);
        }

        /// <summary>Return to the page the comment was posted from (open redirect safe).</summary>
        private IActionResult Back(string? returnUrl, string noticeId, int? reviewId)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Details", "Notices", new { id = noticeId, reviewId });
        }
    }
}
