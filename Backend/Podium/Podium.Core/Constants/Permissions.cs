namespace Podium.Core.Constants
{
    /// <summary>
    /// Application permission constants for band staff
    /// </summary>
    public static class Permissions
    {
        // Student-related permissions
        public const string ViewStudents = "ViewStudents";
        public const string RateStudents = "RateStudents";
        public const string ContactStudents = "ContactStudents";

        // Offer-related permissions
        public const string SendOffers = "SendOffers";
        public const string ManageOffers = "ManageOffers";

        // Event-related permissions
        public const string ManageEvents = "ManageEvents";
        public const string ViewEvents = "ViewEvents";

        // Staff-related permissions
        public const string ManageStaff = "ManageStaff";
        public const string ViewStaff = "ViewStaff";

        // Band-related permissions
        public const string ManageBand = "ManageBand";
        public const string ViewBandDetails = "ViewBandDetails";

        /// <summary>
        /// Get all available permissions
        /// </summary>
        public static string[] GetAllPermissions()
        {
            return new[]
            {
                ViewStudents,
                RateStudents,
                ContactStudents,
                SendOffers,
                ManageOffers,
                ManageEvents,
                ViewEvents,
                ManageStaff,
                ViewStaff,
                ManageBand,
                ViewBandDetails
            };
        }
    }
}