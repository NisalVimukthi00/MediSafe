using System.Text.Json;
using MedSafe.Web.Data;
using MedSafe.Web.Models;

namespace MedSafe.Web.Data
{
    /// <summary>
    /// Creates the SQLite database and seeds the notice dataset, the team accounts and a
    /// small set of demonstration risk reviews / comments so every screen has content on
    /// first run. Safe to call on every start — it only seeds an empty database.
    /// </summary>
    public static class DbSeeder
    {
        private record SeedNotice
        {
            public string id { get; set; } = "";
            public string deviceName { get; set; } = "";
            public string manufacturer { get; set; } = "";
            public string deviceType { get; set; } = "";
            public string model { get; set; } = "";
            public string severity { get; set; } = "";
            public string noticeType { get; set; } = "";
            public string date { get; set; } = "";
            public string status { get; set; } = "";
            public string authority { get; set; } = "";
            public string reference { get; set; } = "";
            public string affectedUnits { get; set; } = "";
            public string distribution { get; set; } = "";
            public string summary { get; set; } = "";
            public string hazard { get; set; } = "";
            public string details { get; set; } = "";
            public string action { get; set; } = "";
            public string keywords { get; set; } = "";
        }

        private record SeedUser
        {
            public string name { get; set; } = "";
            public string email { get; set; } = "";
            public string password { get; set; } = "";
            public string role { get; set; } = "";
            public string? team { get; set; }
            public string? org { get; set; }
        }

        public static void Seed(MedSafeDbContext db, IWebHostEnvironment env)
        {
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var seedPath = Path.Combine(env.ContentRootPath, "Data", "Seed", "notices.json");
            var json = File.ReadAllText(seedPath);
            var records = JsonSerializer.Deserialize<List<SeedNotice>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<SeedNotice>();

            foreach (var r in records)
            {
                db.SafetyNotices.Add(new SafetyNotice
                {
                    Id = r.id,
                    DeviceName = r.deviceName,
                    Manufacturer = r.manufacturer,
                    DeviceType = r.deviceType,
                    Model = r.model,
                    Severity = r.severity,
                    NoticeType = r.noticeType,
                    NoticeDate = DateTime.TryParse(r.date, out var d) ? d : DateTime.UtcNow,
                    Status = r.status,
                    Authority = r.authority,
                    Reference = r.reference,
                    AffectedUnits = r.affectedUnits,
                    Distribution = r.distribution,
                    Summary = r.summary,
                    Hazard = r.hazard,
                    Details = r.details,
                    Action = r.action,
                    Keywords = r.keywords
                });
            }

            // ---- team accounts (employees) and demonstration customer accounts ----
            var users = new List<SeedUser>
            {
                new() { name = "Menula Damyuru", email = "menula@medsafe.com", password = "medsafe123", role = "employee", team = "Account Controlling" },
                new() { name = "Nisal Vimukthi", email = "nisal@medsafe.com", password = "medsafe123", role = "employee", team = "Search" },
                new() { name = "Sulochana Saumya", email = "sulochana@medsafe.com", password = "medsafe123", role = "employee", team = "Sharing & Customer View" },
                new() { name = "Danuja Palihawadana", email = "danuja@medsafe.com", password = "medsafe123", role = "employee", team = "Review & Organise" },
                new() { name = "Vihanga Madushan", email = "vihanga@medsafe.com", password = "medsafe123", role = "employee", team = "Comments & Storage" },
                new() { name = "Dr. Amelia Hart", email = "amelia.hart@medisafe-health.com", password = "customer123", role = "customer", org = "MediSafe Health Group" },
                new() { name = "James Okoro", email = "james.okoro@northbridge.nhs.uk", password = "customer123", role = "customer", org = "Northbridge Hospital Trust" },
                new() { name = "Dr. Sofia Reyes", email = "sofia.reyes@cardiocare-partners.com", password = "customer123", role = "customer", org = "CardioCare Partners" }
            };

            var created = new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase);

            foreach (var s in users)
            {
                var user = new User
                {
                    Name = s.name,
                    Email = s.email,
                    PasswordHash = Services.PasswordHasher.Hash(s.password),
                    Role = s.role == "customer" ? UserRole.Customer : UserRole.Employee,
                    Team = s.team,
                    Organisation = s.org,
                    CreatedAt = new DateTime(2026, 1, 12, 9, 0, 0, DateTimeKind.Utc),
                    CreatedBy = s.role == "customer" ? "Menula Damyuru" : null
                };
                db.Users.Add(user);
                created[s.email] = user;
            }

            db.SaveChanges();

            var menula = created["menula@medsafe.com"];
            var vihanga = created["vihanga@medsafe.com"];
            var nisal = created["nisal@medsafe.com"];
            var amelia = created["amelia.hart@medisafe-health.com"];
            var james = created["james.okoro@northbridge.nhs.uk"];

            // ---- demonstration risk reviews ----
            var review1 = new RiskReview
            {
                Name = "Infusion Pump Portfolio Risk Review — Q3 2026",
                OwnerId = menula.Id,
                Description = "Quarterly review of volumetric and syringe pump field performance ahead of the framework agreement renewal.",
                CreatedAt = new DateTime(2026, 9, 2, 10, 20, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 9, 21, 14, 5, 0, DateTimeKind.Utc)
            };

            var review2 = new RiskReview
            {
                Name = "Cardiac Implant Risk Review — September 2026",
                OwnerId = menula.Id,
                Description = "Review of pacing lead, valve and stent notices for the cardiology portfolio; drives the customer safety bulletin.",
                CreatedAt = new DateTime(2026, 9, 14, 8, 45, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 9, 19, 11, 30, 0, DateTimeKind.Utc)
            };

            var review3 = new RiskReview
            {
                Name = "Imaging & Radiation Safety Review 2026",
                OwnerId = vihanga.Id,
                Description = "Cross-modality imaging review covering MRI thermal risk, CT contrast injection and X-ray collimation.",
                CreatedAt = new DateTime(2026, 8, 28, 13, 10, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 9, 8, 16, 40, 0, DateTimeKind.Utc)
            };

            db.RiskReviews.AddRange(review1, review2, review3);
            db.SaveChanges();

            void Link(RiskReview review, params string[] noticeIds)
            {
                foreach (var id in noticeIds)
                {
                    if (db.SafetyNotices.Any(n => n.Id == id))
                        db.ReviewNotices.Add(new ReviewNotice { RiskReviewId = review.Id, SafetyNoticeId = id });
                }
            }

            Link(review1, "MS-2026-001", "MS-2025-026", "MS-2026-013");
            Link(review2, "MS-2026-014", "MS-2025-041", "MS-2025-012");
            Link(review3, "MS-2026-004", "MS-2025-017", "MS-2024-018");

            db.ReviewShares.AddRange(
                new ReviewShare
                {
                    RiskReviewId = review1.Id, CustomerId = amelia.Id,
                    SharedAt = new DateTime(2026, 9, 21, 14, 6, 0, DateTimeKind.Utc), SharedBy = "Menula Damyuru"
                },
                new ReviewShare
                {
                    RiskReviewId = review1.Id, CustomerId = james.Id,
                    SharedAt = new DateTime(2026, 9, 21, 14, 7, 0, DateTimeKind.Utc), SharedBy = "Menula Damyuru"
                },
                new ReviewShare
                {
                    RiskReviewId = review3.Id, CustomerId = james.Id,
                    SharedAt = new DateTime(2026, 9, 8, 16, 41, 0, DateTimeKind.Utc), SharedBy = "Vihanga Madushan"
                });

            void Comment(string noticeId, int? reviewId, User author, string text, DateTime at)
            {
                if (!db.SafetyNotices.Any(n => n.Id == noticeId)) return;
                db.Comments.Add(new Comment
                {
                    SafetyNoticeId = noticeId,
                    RiskReviewId = reviewId,
                    AuthorId = author.Id,
                    AuthorRole = author.Role,
                    Text = text,
                    CreatedAt = at,
                    UpdatedAt = at
                });
            }

            Comment("MS-2026-001", review1.Id, menula,
                "Uncommanded bolus is the highest-priority finding for this review. The affected pumps are used for heparin and insulin in our partner trust, so the risk score should be recorded as critical with immediate mitigation.",
                new DateTime(2026, 9, 2, 11, 2, 0, DateTimeKind.Utc));

            Comment("MS-2026-001", review1.Id, vihanga,
                "Confirmed with the supplier that firmware 5.4 will be released by 12 October. Until then I have recommended blocking reprogramming while an occlusion alarm is active — added to the interim guidance note.",
                new DateTime(2026, 9, 5, 9, 41, 0, DateTimeKind.Utc));

            Comment("MS-2025-026", review1.Id, nisal,
                "Syringe recognition failure is confined to the neonatal setting in our incident data. Two clinicians must confirm the recognised size until firmware 3.11 is installed.",
                new DateTime(2026, 9, 4, 15, 20, 0, DateTimeKind.Utc));

            Comment("MS-2026-001", review1.Id, amelia,
                "Thank you — we will withdraw the affected pumps from our high-alert medicines cupboard this week and log the interim guidance with our medicines safety officer. Could you confirm whether the replacement units are covered under the existing service contract?",
                new DateTime(2026, 9, 22, 9, 15, 0, DateTimeKind.Utc));

            Comment("MS-2026-014", review2.Id, menula,
                "Lead fracture rate is well above expectation and pacing-dependent patients are the exposure group. Do not explant prophylactically — arrange impedance checks first so we avoid unnecessary revision surgery.",
                new DateTime(2026, 9, 15, 10, 5, 0, DateTimeKind.Utc));

            Comment("MS-2026-004", review3.Id, vihanga,
                "Coolant circuit cleaning booked for both scanners. Interim thermal pad and 45-minute monitoring limit communicated to the radiography team.",
                new DateTime(2026, 9, 1, 12, 30, 0, DateTimeKind.Utc));

            db.SaveChanges();
        }
    }
}
