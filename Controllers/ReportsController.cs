using ANDtr.Web.Data;
using ANDtr.Web.Models;
using ANDtr.Web.Services;
using MedSafe.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ANDtr.Web.Controllers
{

    /// Report generation and dataset export. The PDF contains the review header, every
    /// selected notice with its full dataset record and all comments for each notice.

    public class ReportsController : BaseController
    {
        private readonly PdfReportService _pdf;

        public ReportsController(ANDtrDbContext db, CurrentUserService current, SearchService search, PdfReportService pdf)
            : base(db, current, search)
        {
            _pdf = pdf;
        }

        public IActionResult Index()
        {
            var guard = RequireEmployee();
            if (guard != null) return guard;

            ViewBag.Active = "reports";

            var reviews = Db.RiskReviews
                .Include(r => r.Owner)
                .Include(r => r.Notices)
                .Where(r => r.OwnerId == Me!.Id)
                .OrderByDescending(r => r.UpdatedAt)
                .ToList();

            ViewBag.CommentCounts = Db.Comments
                .Where(c => c.RiskReviewId != null)
                .GroupBy(c => c.RiskReviewId!.Value)
                .Select(g => new { ReviewId = g.Key, Count = g.Count() })
                .ToDictionary(x => x.ReviewId, x => x.Count);

            return View(reviews);
        }

        public IActionResult Pdf(int id)
        {
            var guard = RequireEmployee();
            if (guard != null) return guard;

            var review = Db.RiskReviews
                .Include(r => r.Owner)
                .Include(r => r.Notices).ThenInclude(rn => rn.SafetyNotice)
                .Include(r => r.Shares).ThenInclude(s => s.Customer)
                .FirstOrDefault(r => r.Id == id && r.OwnerId == Me!.Id);

            if (review == null) return NotFound();

            var notices = review.Notices
                .Select(rn => rn.SafetyNotice!)
                .Where(n => n != null)
                .OrderBy(n => n.SeverityRank)
                .ThenBy(n => n.NoticeDate)
                .ToList();

            var comments = Db.Comments
                .Include(c => c.Author)
                .Where(c => c.RiskReviewId == id)
                .OrderBy(c => c.CreatedAt)
                .ToList();

            var shared = review.Shares
                .Where(s => !s.HiddenByCustomer && s.Customer != null)
                .Select(s => s.Customer!)
                .ToList();

            var bytes = _pdf.Build(review, notices, comments, shared);

            var safeName = new string(review.Name
                .Where(ch => char.IsLetterOrDigit(ch) || ch == ' ' || ch == '-')
                .ToArray()).Trim().Replace(' ', '-');

            return File(bytes, "application/pdf", $"ANDtr-Risk-Review-{safeName}.pdf");
        }

        /// Full database export for backup / hand-over.
        public IActionResult ExportJson()
        {
            var guard = RequireEmployee();
            if (guard != null) return guard;

            var payload = new
            {
                platform = "ANDtr Recall Platform",
                exportedAt = DateTime.UtcNow,
                notices = Db.SafetyNotices.AsNoTracking().OrderBy(n => n.Id).Select(n => new
                {
                    n.Id,
                    n.DeviceName,
                    n.Manufacturer,
                    n.DeviceType,
                    n.Model,
                    n.Severity,
                    n.NoticeType,
                    date = n.NoticeDate.ToString("yyyy-MM-dd"),
                    n.Status,
                    n.Authority,
                    n.Reference,
                    n.AffectedUnits,
                    n.Distribution,
                    n.Summary,
                    n.Hazard,
                    n.Details,
                    n.Action,
                    n.Keywords
                }),
                users = Db.Users.AsNoTracking().Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    role = u.Role == UserRole.Employee ? "employee" : "customer",
                    u.Team,
                    u.Organisation,
                    u.CreatedAt,
                    u.CreatedBy
                }),
                reviews = Db.RiskReviews.AsNoTracking().Include(r => r.Notices).Include(r => r.Shares).Select(r => new
                {
                    r.Id,
                    r.Name,
                    owner = r.Owner!.Name,
                    r.Description,
                    r.CreatedAt,
                    r.UpdatedAt,
                    notices = r.Notices.Select(x => x.SafetyNoticeId),
                    shares = r.Shares.Select(s => new { customer = s.Customer!.Email, s.SharedAt, s.SharedBy, s.HiddenByCustomer })
                }),
                comments = Db.Comments.AsNoTracking().Select(c => new
                {
                    c.Id,
                    c.SafetyNoticeId,
                    c.RiskReviewId,
                    author = c.Author!.Name,
                    role = c.AuthorRole == UserRole.Employee ? "employee" : "customer",
                    c.Text,
                    c.CreatedAt,
                    c.UpdatedAt
                })
            };

            var json = System.Text.Json.JsonSerializer.Serialize(payload,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

            return File(System.Text.Encoding.UTF8.GetBytes(json),
                "application/json", $"andtr-database-{DateTime.UtcNow:yyyyMMdd}.json");
        }

        /// Spreadsheet-friendly CSV of the notice dataset.
        public IActionResult ExportCsv()
        {
            var guard = RequireEmployee();
            if (guard != null) return guard;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("NoticeId,DeviceName,Manufacturer,DeviceType,Model,Severity,NoticeType,Date,Status,Authority,Reference,AffectedUnits,Distribution,Summary,Hazard,Details,Action,Keywords");

            static string Q(string? v) => "\"" + (v ?? string.Empty).Replace("\"", "\"\"") + "\"";

            foreach (var n in Db.SafetyNotices.AsNoTracking().OrderBy(n => n.Id))
            {
                sb.AppendLine(string.Join(',',
                    Q(n.Id), Q(n.DeviceName), Q(n.Manufacturer), Q(n.DeviceType), Q(n.Model),
                    Q(n.Severity), Q(n.NoticeType), Q(n.NoticeDate.ToString("yyyy-MM-dd")),
                    Q(n.Status), Q(n.Authority), Q(n.Reference), Q(n.AffectedUnits),
                    Q(n.Distribution), Q(n.Summary), Q(n.Hazard), Q(n.Details), Q(n.Action), Q(n.Keywords)));
            }

            return File(System.Text.Encoding.UTF8.GetBytes(sb.ToString()), "text/csv",
                $"andtr-notices-{DateTime.UtcNow:yyyyMMdd}.csv");
        }
    }
}