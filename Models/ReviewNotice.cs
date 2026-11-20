using System.ComponentModel.DataAnnotations.Schema;

namespace ANDtr.Web.Models
{
    /// <summary>
    /// Join entity between a risk review and a notice. A notice can belong to many
    /// reviews, and a review can hold many notices — never the same notice twice.
    /// </summary>
    public class ReviewNotice
    {
        public int RiskReviewId { get; set; }
        public RiskReview? RiskReview { get; set; }

        public string SafetyNoticeId { get; set; } = string.Empty;
        public SafetyNotice? SafetyNotice { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}
