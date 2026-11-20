namespace ANDtr.Web.Models
{
    /// <summary>
    /// The employee's "selection bucket": notices ticked while searching and waiting to be
    /// grouped into a risk review. Stored per user so the selection survives navigation
    /// and log out, and so a notice can never be selected twice.
    /// </summary>
    public class SelectedNotice
    {
        public int UserId { get; set; }
        public User? User { get; set; }

        public string SafetyNoticeId { get; set; } = string.Empty;
        public SafetyNotice? SafetyNotice { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}
