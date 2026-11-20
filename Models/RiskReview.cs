using System.ComponentModel.DataAnnotations;

namespace ANDtr.Web.Models
{
    /// A named collection of notice findings ("risk review") owned by an employee.
    /// Reviews are stored in the database so findings survive logging out and back in.
    public class RiskReview
    {
        public int Id { get; set; }

        [Required, StringLength(160)]
        public string Name { get; set; } = string.Empty;

        public int OwnerId { get; set; }
        public User? Owner { get; set; }

        [StringLength(600)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ReviewNotice> Notices { get; set; } = new List<ReviewNotice>();
        public ICollection<ReviewShare> Shares { get; set; } = new List<ReviewShare>();

        public int NoticeCount => Notices?.Count ?? 0;

        /// Shares that are still visible to the customer.
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public IEnumerable<ReviewShare> ActiveShares =>
            (Shares ?? new List<ReviewShare>()).Where(s => !s.HiddenByCustomer);

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool IsShared => ActiveShares.Any();
    }
}
