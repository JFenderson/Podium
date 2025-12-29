using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Core.Entities
{
    public class SearchAlert
    {
        public int Id { get; set; }
        public int SavedSearchId { get; set; }
        public int NewMatchesCount { get; set; }
        public DateTime SentAt { get; set; }
        public bool WasEmailSent { get; set; }
        public string? EmailError { get; set; }

        // Sample of new matches (comma-separated student IDs)
        public string? NewMatchIds { get; set; }

        // Navigation
        public SavedSearch SavedSearch { get; set; } = null!;
    }
}
