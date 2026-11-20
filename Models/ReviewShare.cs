using System.ComponentModel.DataAnnotations;

namespace ANDtr.Web.Models
{
    /// <summary>
    /// Grants one customer account read access to one risk review. A customer can only
    /// ever open reviews that have a share row for their own account.
    /// </summary>
    public class ReviewShare
    {
        public int Id { get; set; }

        public int RiskReviewId { get; set; }
        public RiskReview? RiskReview { get; set; }

        public int CustomerId { get; set; }
        public User? Customer { get; set; }

        public DateTime SharedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Name of the employee who shared the review.</summary>
        [StringLength(120)]
        public string SharedBy { get; set; } = string.Empty;

        /// <summary>
        /// Set when the customer removes the finding from their own list. The audit link
        /// with the employee is kept, but the review no longer appears for that customer.
        /// </summary>
        public bool HiddenByCustomer { get; set; }
    }
}
