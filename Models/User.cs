using System.ComponentModel.DataAnnotations;

namespace ANDtr.Web.Models
{
    /// <summary>
    /// The two account types supported by the platform. Employees analyse notices,
    /// customers only ever see the findings that have been shared with them.
    /// </summary>
    public enum UserRole
    {
        Employee = 0,
        Customer = 1
    }

    /// <summary>
    /// A platform account. Employees are created with the system; customer accounts
    /// are created by an employee from the Customers page.
    /// </summary>
    public class User
    {
        public int Id { get; set; }

        [Required, StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(180)]
        public string Email { get; set; } = string.Empty;

        /// <summary>PBKDF2 hash of the account password (salt + hash, Base64).</summary>
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public UserRole Role { get; set; }

        /// <summary>Employee team, e.g. "Search". Null for customers.</summary>
        [StringLength(80)]
        public string? Team { get; set; }

        /// <summary>Customer organisation. Null for employees.</summary>
        [StringLength(160)]
        public string? Organisation { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Name of the employee who created a customer account.</summary>
        [StringLength(120)]
        public string? CreatedBy { get; set; }

        public ICollection<RiskReview> Reviews { get; set; } = new List<RiskReview>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();

        public bool IsEmployee => Role == UserRole.Employee;
        public bool IsCustomer => Role == UserRole.Customer;

        /// <summary>Two letter initials used by the avatar circles.</summary>
        public string Initials
        {
            get
            {
                var cleaned = Name;
                foreach (var prefix in new[] { "Dr.", "Mr.", "Ms.", "Mrs." })
                {
                    if (cleaned.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        cleaned = cleaned.Substring(prefix.Length).Trim();
                        break;
                    }
                }

                var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) return "?";
                if (parts.Length == 1) return parts[0].Substring(0, 1).ToUpperInvariant();
                return (parts[0].Substring(0, 1) + parts[^1].Substring(0, 1)).ToUpperInvariant();
            }
        }

        public string FirstName => Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? Name;
    }
}
