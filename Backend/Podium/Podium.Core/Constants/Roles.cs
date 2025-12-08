namespace Podium.Core.Constants
{
    /// <summary>
    /// Application role constants
    /// </summary>
    public static class Roles
    {
        public const string Student = "Student";
        public const string Guardian = "Guardian";
        public const string Recruiter = "Recruiter";
        public const string Director = "Director";
        public const string Admin = "Admin";

        /// <summary>
        /// Get all available roles
        /// </summary>
        public static string[] GetAllRoles()
        {
            return new[] { Student, Guardian, Recruiter, Director, Admin };
        }

        /// <summary>
        /// Check if a role is a band staff role
        /// </summary>
        public static bool IsBandStaffRole(string role)
        {
            return role == Recruiter || role == Director;
        }
    }
}
