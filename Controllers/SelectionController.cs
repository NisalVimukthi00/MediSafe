using MedSafe.Web.Data;
using MedSafe.Web.Models;
using MedSafe.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedSafe.Web.Controllers
{
    /// <summary>
    /// The employee's selection bucket: notices ticked while searching that are waiting to
    /// be grouped into a named risk review. A notice can be selected from the results list
    /// or its details page, deselected at any time, and is never added twice.
    /// </summary>
    public class SelectionController : BaseController
    {
        public SelectionController(MedSafeDbContext db, CurrentUserService current, SearchService search)
            : base(db, current, search) { }

        public IActionResult Index()
        {
            var guard = RequireEmployee();
            if (guard != null) return guard;

            ViewBag.Active = "selection";

            var items = Db.SelectedNotices
                .Include(s => s.SafetyNotice)
                .Where(s => s.UserId == Me!.Id)
                .OrderByDescending(s => s.AddedAt)
                .Select(s => s.SafetyNotice!)
                .ToList();

            return View(items);
        }

        /// <summary>Tick / untick a notice. Posting the same id twice is a no-op.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Toggle(string noticeId, string? returnUrl)
        {
            var guard = RequireEmployee();
            if (guard != null) return guard;

            var me = Me!;
            var existing = Db.SelectedNotices.FirstOrDefault(s => s.UserId == me.Id && s.SafetyNoticeId == noticeId);

            if (existing != null)
            {
                Db.SelectedNotices.Remove(existing);
                Db.SaveChanges();
            }
            else if (Db.SafetyNotices.Any(n => n.Id == noticeId))
            {
                Db.SelectedNotices.Add(new SelectedNotice
                {
                    UserId = me.Id,
                    SafetyNoticeId = noticeId,
                    AddedAt = DateTime.UtcNow
                });
                Db.SaveChanges();
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Clear(string? returnUrl)
        {
            var guard = RequireEmployee();
            if (guard != null) return guard;

            var items = Db.SelectedNotices.Where(s => s.UserId == Me!.Id).ToList();
            Db.SelectedNotices.RemoveRange(items);
            Db.SaveChanges();

            Notify("Selection cleared.");

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Index");
        }
    }
}
