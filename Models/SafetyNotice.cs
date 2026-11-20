using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace ANDtr.Web.Models
{
    /// <summary>
    /// One record from the provided medical device safety / recall notice dataset.
    /// The string primary key is the notice identifier published by the authority
    /// (e.g. ANDTR-2026-014) so records stay traceable to the dataset.
    /// </summary>
    public class SafetyNotice
    {
        [Key, StringLength(32)]
        public string Id { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string DeviceName { get; set; } = string.Empty;

        [Required, StringLength(120)]
        public string Manufacturer { get; set; } = string.Empty;

        [StringLength(80)]
        public string DeviceType { get; set; } = string.Empty;

        [StringLength(80)]
        public string Model { get; set; } = string.Empty;

        /// <summary>Critical, High, Medium or Low.</summary>
        [StringLength(20)]
        public string Severity { get; set; } = string.Empty;

        /// <summary>Recall, Field Safety Notice, Safety Alert ...</summary>
        [StringLength(60)]
        public string NoticeType { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime NoticeDate { get; set; }

        [StringLength(40)]
        public string Status { get; set; } = string.Empty;

        /// <summary>Regulatory authority, e.g. MHRA or FDA.</summary>
        [StringLength(60)]
        public string Authority { get; set; } = string.Empty;

        [StringLength(60)]
        public string Reference { get; set; } = string.Empty;

        [StringLength(40)]
        public string AffectedUnits { get; set; } = string.Empty;

        [StringLength(240)]
        public string Distribution { get; set; } = string.Empty;

        public string Summary { get; set; } = string.Empty;
        public string Hazard { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;

        /// <summary>Space separated index terms used by the search engine.</summary>
        public string Keywords { get; set; } = string.Empty;

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<ReviewNotice> ReviewLinks { get; set; } = new List<ReviewNotice>();
        public ICollection<SelectedNotice> Selections { get; set; } = new List<SelectedNotice>();

        public int SeverityRank => Severity switch
        {
            "Critical" => 4,
            "High" => 3,
            "Medium" => 2,
            "Low" => 1,
            _ => 0
        };

        /// <summary>CSS modifier used by the severity badge (b-critical, b-high ...).</summary>
        public string SeverityClass =>
            "b-" + new string((Severity ?? string.Empty).ToLowerInvariant().Where(char.IsLetter).ToArray());

        public string[] KeywordList =>
            (Keywords ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }
}
