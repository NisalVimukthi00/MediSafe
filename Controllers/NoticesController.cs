using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AndtrMedicalSafety.Data;
using AndtrMedicalSafety.Models;
using AndtrMedicalSafety.Services;
using AndtrMedicalSafety.ViewModels;

namespace AndtrMedicalSafety.Controllers;

/// <summary>
/// Controller for viewing medical recall notice details, managing selection buffer, and attaching notices to risk reviews.
/// </summary>
[Authorize(Roles = "Employee")]
public class NoticesController : Controller
{
    private readonly AppDbContext _context;
    private readonly ISelectionService _selectionService;

    public NoticesController(AppDbContext context, ISelectionService selectionService)
    {
        _context = context;
        _selectionService = selectionService;
    }

    // --- Figure 2.11: Notice Details Page ---
    [HttpGet]
    public async Task<IActionResult> Details(int id, string? returnUrl = null)
    {
        var notice = await _context.Notices
            .FirstOrDefaultAsync(n => n.NoticeId == id);

        if (notice == null)
            return NotFound();

        int currentUserId = GetCurrentUserId();
        bool isSelected = await _selectionService.IsNoticeSelectedAsync(currentUserId, id);

        // Fetch all comments on this notice
        var comments = await _context.Comments
            .Include(c => c.User)
            .Where(c => c.NoticeId == id)
            .OrderByDescending(c => c.CreatedDate)
            .ToListAsync();

        // Get reviews belonging to or accessible by this employee
        var employeeReviews = await _context.RiskReviews
            .Where(r => r.EmployeeId == currentUserId)
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync();

        // Check if notice is already associated with any reviews
        var associatedReviewIds = await _context.Findings
            .Where(f => f.NoticeId == id)
            .Select(f => f.ReviewId)
            .Distinct()
            .ToListAsync();

        var viewModel = new NoticeDetailsViewModel
        {
            Notice = notice,
            IsSelected = isSelected,
            Comments = comments,
            ExistingRiskReviews = employeeReviews,
            AssociatedReviewIds = associatedReviewIds,
            ReturnUrl = returnUrl,
            CurrentUserId = currentUserId,
            CurrentUserRole = User.FindFirstValue(ClaimTypes.Role) ?? "Employee"
        };

        return View(viewModel);
    }

    // --- Figure 2.8: Selected Notices Page ---
    [HttpGet]
    public async Task<IActionResult> Selected()
    {
        int currentUserId = GetCurrentUserId();
        var selectedNotices = await _selectionService.GetSelectedNoticesAsync(currentUserId);

        var viewModel = new SelectedNoticesViewModel
        {
            SelectedNotices = selectedNotices,
            NewReview = new CreateRiskReviewInputModel
            {
                SelectedNoticeIds = selectedNotices.Select(n => n.NoticeId).ToList()
            }
        };

        return View(viewModel);
    }

    // --- AJAX / Form Toggle Selection ---
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSelect(int noticeId, string? returnUrl = null)
    {
        int currentUserId = GetCurrentUserId();
        bool isNowSelected = await _selectionService.ToggleSelectNoticeAsync(currentUserId, noticeId);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            int count = await _selectionService.GetSelectedCountAsync(currentUserId);
            return Json(new { success = true, isSelected = isNowSelected, totalSelected = count });
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(nameof(Selected));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deselect(int noticeId, string? returnUrl = null)
    {
        int currentUserId = GetCurrentUserId();
        await _selectionService.DeselectNoticeAsync(currentUserId, noticeId);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(nameof(Selected));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearSelection()
    {
        int currentUserId = GetCurrentUserId();
        await _selectionService.ClearSelectionsAsync(currentUserId);
        TempData["Success"] = "All notice selections cleared.";
        return RedirectToAction(nameof(Selected));
    }

    // Add notice directly to existing Risk Review
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToReview(int noticeId, int reviewId, string? reasoning, int relevanceRating = 3)
    {
        var review = await _context.RiskReviews
            .Include(r => r.Findings)
            .FirstOrDefaultAsync(r => r.ReviewId == reviewId);

        if (review == null)
        {
            TempData["Error"] = "Selected risk review was not found.";
            return RedirectToAction(nameof(Details), new { id = noticeId });
        }

        // Avoid duplicate findings
        bool alreadyAdded = review.Findings.Any(f => f.NoticeId == noticeId);
        if (alreadyAdded)
        {
            TempData["Warning"] = "This notice is already included in the selected Risk Review.";
            return RedirectToAction(nameof(Details), new { id = noticeId });
        }

        var finding = new Finding
        {
            ReviewId = reviewId,
            NoticeId = noticeId,
            RelevanceRating = relevanceRating,
            Reasoning = reasoning ?? "Identified during regulatory review",
            CreatedDate = DateTime.UtcNow
        };

        _context.Findings.Add(finding);
        review.UpdatedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Notice successfully added to '{review.Title}'.";
        return RedirectToAction("Details", "RiskReviews", new { id = reviewId });
    }

    private int GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idClaim, out int id) ? id : 0;
    }
}
