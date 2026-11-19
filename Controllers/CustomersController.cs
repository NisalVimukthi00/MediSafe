using ANDtr.Web.Data;
using ANDtr.Web.Models;
using ANDtr.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ANDtr.Web.Controllers
{
    /// <summary>
    /// Customer account administration. Employees create the account; the customer can then
    /// sign in with the default password and only ever sees reviews shared with them.
    /// </summary>
    public class CustomersController : BaseController
    {
        public const string DefaultCustomerPassword = "customer123";

        public CustomersController(ANDtrDbContext db, CurrentUserService current, SearchService search)
            : base(db, current, search) { }

        public IActionResult Index()
        {
            var guard = RequireEmployee();
            if (guard != null) return guard;

            ViewBag.Active = "customers";

            var customers = Db.Users
                .Where(u => u.Role == UserRole.Customer)
                .OrderBy(u => u.Name)
                .ToList();

            var shareCounts = Db.ReviewShares
                .Where(s => !s.HiddenByCustomer)
                .GroupBy(s => s.CustomerId)
                .Select(g => new { CustomerId = g.Key, Count = g.Count() })
                .ToDictionary(x => x.CustomerId, x => x.Count);

            ViewBag.ShareCounts = shareCounts;
            ViewBag.Reviews = Db.RiskReviews.Include(r => r.Shares)
                .Where(r => r.OwnerId == Me!.Id)
                .OrderBy(r => r.Name)
                .ToList();

            return View(customers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string name, string email, string? organisation, int? shareReviewId)
        {
            var guard = RequireEmployee();
            if (guard != null) return guard;

            if (string.IsNullOrWhiteSpace(name))
            {
                Notify("Enter the customer's name.", "err");
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@') || !email.Contains('.'))
            {
                Notify("Enter a valid email address.", "err");
                return RedirectToAction("Index");
            }

            var normalised = email.Trim().ToLowerInvariant();

            if (Db.Users.Any(u => u.Email.ToLower() == normalised))
            {
                Notify("An account with that email already exists.", "warn");
                return RedirectToAction("Index");
            }

            var customer = new User
            {
                Name = name.Trim(),
                Email = normalised,
                PasswordHash = PasswordHasher.Hash(DefaultCustomerPassword),
                Role = UserRole.Customer,
                Organisation = string.IsNullOrWhiteSpace(organisation) ? null : organisation.Trim(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Me!.Name
            };

            Db.Users.Add(customer);
            Db.SaveChanges();

            Notify($"Customer account created for {customer.Name}. Default password: {DefaultCustomerPassword}.");

            // optional convenience: share one of the employee's reviews straight away
            if (shareReviewId.HasValue)
            {
                var review = Db.RiskReviews.FirstOrDefault(r => r.Id == shareReviewId.Value && r.OwnerId == Me.Id);
                if (review != null)
                {
                    Db.ReviewShares.Add(new ReviewShare
                    {
                        RiskReviewId = review.Id,
                        CustomerId = customer.Id,
                        SharedAt = DateTime.UtcNow,
                        SharedBy = Me.Name
                    });
                    Db.SaveChanges();
                    Notify($"Customer account created for {customer.Name} and \"{review.Name}\" shared with them.");
                }
            }

            return RedirectToAction("Index");
        }
    }
}
