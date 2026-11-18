# MedSafe — Medical Device Safety & Recall Notice Platform

ASP.NET MVC (.NET 10) implementation of the Live Brief scenario and the 15 user stories in
`USER STORIES.docx`. Rebranded end-to-end from *ANDtr* to **MedSafe**.

---

## 1. Open and run in Visual Studio

1. Install **Visual Studio 2022** (17.8+) with the **ASP.NET and web development** workload, and
   the **.NET 10.0 SDK**.
2. Open `MedSafe.sln`.
3. Press **F5**. The browser opens at `http://localhost:5080`.

On first run the app creates **`medsafe.db`** (SQLite) next to the project, applies the schema and
seeds the 33-notice dataset, 8 accounts, 3 demonstration risk reviews and 6 comments.
Findings therefore survive log out, restart and redeploy — the "like saving a Word document"
acceptance criterion.

### Command line

```bash
dotnet restore
dotnet run --project src/MedSafe.Web
```

To start from a clean database, stop the app and delete `src/MedSafe.Web/medsafe.db`.

---

## 2. Demo accounts

| Role | Email | Password |
|---|---|---|
| Employee — Account Controlling | `menula@medsafe.com` | `medsafe123` |
| Employee — Search | `nisal@medsafe.com` | `medsafe123` |
| Employee — Sharing & Customer View | `sulochana@medsafe.com` | `medsafe123` |
| Employee — Review & Organise | `danuja@medsafe.com` | `medsafe123` |
| Employee — Comments & Storage | `vihanga@medsafe.com` | `medsafe123` |
| Customer | `amelia.hart@medisafe-health.com` | `customer123` |
| Customer | `james.okoro@northbridge.nhs.uk` | `customer123` |
| Customer | `sofia.reyes@cardiocare-partners.com` | `customer123` |

Customer accounts created from the **Customers** page start with the default password
`customer123`. Passwords are stored as PBKDF2-SHA256 hashes (100 000 iterations), never plain text.

> `medisafe-health.com` / `northbridge.nhs.uk` are customer organisations from the original
> dataset, not the platform brand.

---

## 3. Project structure

```
MedSafe.sln
README.md
src/MedSafe.Web/
├─ Program.cs                     Composition root: services, EF Core, session, seeding
├─ appsettings.json               SQLite connection string
├─ MedSafe.Web.csproj
├─ Controllers/
│  ├─ BaseController.cs           Session load, sidebar counters, employee/customer guards
│  ├─ AccountController.cs        Sign in / sign out, demo credentials
│  ├─ HomeController.cs           Employee + customer dashboards, status pages
│  ├─ NoticesController.cs        Search, autocomplete, notice details, print view
│  ├─ SelectionController.cs      Selection bucket (add / remove / clear)
│  ├─ ReviewsController.cs        Create, rename, delete, add/remove notices, share, unshare
│  ├─ CommentsController.cs       Add, edit, delete comments (own comments only)
│  ├─ CustomersController.cs      Create customer accounts
│  ├─ SharedFindingsController.cs Customer portal (shared findings only)
│  ├─ ReportsController.cs        PDF report, JSON + CSV export
│  ├─ ReferenceController.cs      User-story coverage, dataset page, data reset
│  └─ SharedController.cs         /Shared alias → customer portal
├─ Models/
│  ├─ User.cs                     Account + role enum (Employee / Customer)
│  ├─ SafetyNotice.cs             One dataset record (string key = notice ID)
│  ├─ RiskReview.cs               Named findings container
│  ├─ ReviewNotice.cs             Review ↔ notice join (composite key)
│  ├─ ReviewShare.cs              Review ↔ customer share (unique per pair)
│  ├─ Comment.cs                  Employee / customer comment with role snapshot
│  └─ SelectedNotice.cs           Per-user selection bucket (composite key)
├─ Data/
│  ├─ MedSafeDbContext.cs         Fluent model: keys, indexes, cascades, unique email
│  ├─ DbSeeder.cs                 First-run seed (dataset, accounts, demo reviews)
│  └─ Seed/
│     ├─ notices.json             33 notices, IDs rebranded MS-YYYY-NNN
│     └─ user-stories.json        Requirement text for the coverage page
├─ Services/
│  ├─ SearchService.cs            Weighted scoring, filters, sort, typo, related terms, suggestions
│  ├─ PdfReportService.cs         Dependency-free PDF 1.4 writer for review reports
│  ├─ CurrentUserService.cs       Session-backed current account
│  ├─ PasswordHasher.cs           PBKDF2-SHA256
│  ├─ Mark.cs                     HTML-encode then <mark> highlight (no XSS)
│  ├─ Format.cs                   en-GB dates, relative times, normalisation
│  └─ DemoAccounts.cs             Demo credentials shown on the sign-in screen
├─ Views/                         Razor views, partials and layouts
└─ wwwroot/
   ├─ css/site.css                Full design system (navy / teal), responsive, print styles
   └─ js/site.js                  Progressive enhancement: suggestions, toggles, autosubmit
```

---

## 4. How the requirement document is covered

| Story | Where it lives |
|---|---|
| Login | `AccountController.Login`, session cookie, role badge |
| Create customer account | `CustomersController.Create` (name, email, org, optional share) |
| Role permissions | `BaseController.RequireEmployee/RequireCustomer`, `CanSeeReview` |
| Search notices | `NoticesController.Search` + `SearchService` (weights, filters, sort) |
| Typo tolerance / suggestions | Bounded Levenshtein correction, related-term expansion, `/Notices/Suggest` |
| View notice details | `NoticesController.Details`, all 17 dataset fields |
| Select notices | `SelectionController.Toggle`, composite key prevents duplicates |
| Create / organise reviews | `ReviewsController` create, rename, add, remove, delete |
| Comments and storage | `CommentsController`, employee and customer sections kept apart |
| Share with customers | `ReviewsController.Share/Unshare`, `ReviewShare` grant table |
| Customer view | `SharedFindingsController`, `Reviews/Details` read-only for customers |
| Customer comments | Same composer, labelled `Customer`, visible to employees |
| Persistence | SQLite file, plus a log-out / log-in test link on the Data page |
| PDF reports | `PdfReportService` — header, notices, findings, comments, sign-off, page numbers |
| Coverage / traceability | `/Reference/Coverage` renders every story and criterion |

The **User Story Coverage** page is generated from `Data/Seed/user-stories.json`, so the
requirement text is not hard-coded in a view.

---

## 5. Security notes

- Every POST is protected by `@Html.AntiForgeryToken()` + `[ValidateAntiForgeryToken]`.
- Authorisation is enforced server-side: role guards on each action, ownership checks in
  `OwnedReview()`, and `CanSeeReview()` for customer access — a customer cannot reach an
  unshared review by editing a URL or a form field.
- Comments may only be edited/deleted by their author.
- All notice text is HTML-encoded before highlighting, so dataset content cannot inject markup.
- `returnUrl` redirects are validated with `Url.IsLocalUrl`.

---

## 6. Known scope notes

- Reports are generated by a self-contained PDF writer rather than a headless browser, so the
  server needs no extra runtime; layout is a single column with page numbering.
- The seeded customer emails keep their original organisations (`medisafe-health.com`,
  `northbridge.nhs.uk`, `cardiocare-partners.com`).
- `medsafe.db` is created on first run; it is deliberately not committed.
