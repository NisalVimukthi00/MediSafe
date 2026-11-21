using System.ComponentModel.DataAnnotations;

namespace ANDtr.Web.Models
{
    /// <summary>
    /// A comment recorded against a notice. When ReviewId is set the comment belongs to
    /// that risk review (this is how employee findings and customer feedback are stored).
    /// Employee and customer comments are always displayed in separate sections.
    /// </summary>
    public class Comment
    {
        public int Id { get; set; }

        [Required, StringLength(32)]
        public string SafetyNoticeId { get; set; } = string.Empty;
        public SafetyNotice? SafetyNotice { get; set; }

        public int? RiskReviewId { get; set; }
        public RiskReview? RiskReview { get; set; }

        public int AuthorId { get; set; }
        public User? Author { get; set; }

        /// <summary>Snapshot of the author role so history keeps its label.</summary>
        public UserRole AuthorRole { get; set; }

        [Required, StringLength(4000)]
        public string Text { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsCustomerComment => AuthorRole == UserRole.Customer;
        public bool WasEdited => UpdatedAt > CreatedAt.AddSeconds(1);
    }
}
