using Podium.Core.Entities;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Podium.Core.Interfaces
{
    /// <summary>
    /// Unit of Work pattern for coordinating repository operations
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        IRepository<RefreshToken> RefreshTokens { get; }
        IRepository<Student> Students { get; }
        IRepository<Guardian> Guardians { get; }
        IRepository<BandStaff> BandStaff { get; }
        IRepository<Band> Bands { get; }
        IRepository<Video> Videos { get; }
        IRepository<VideoRating> VideoRatings { get; }
        IRepository<ScholarshipOffer> ScholarshipOffers { get; }
        IRepository<AuditLog> AuditLogs { get; }
        IRepository<BandEvent> BandEvents { get; }
        IRepository<EventRegistration> EventRegistrations { get; }
        IRepository<Notification> Notifications { get; }
        IRepository<StudentInterest>? StudentInterests { get; }
        IRepository<StudentRating>? StudentRatings { get; }
        IRepository<StudentGuardian>? StudentGuardians { get; }
        IRepository<BandBudget> BandBudgets { get; }
        IRepository<ContactRequest> ContactRequests { get; }
        IRepository<ContactLog> ContactLogs { get; }
        IRepository<GuardianNotification> GuardianNotifications { get; }
        IRepository<GuardianNotificationPreferences> GuardianNotificationPreferences { get; }
        IRepository<SavedSearch> SavedSearches { get; }
        IRepository<SearchAlert> SearchAlerts { get; }
        /// <summary>
        /// Save all changes to the database
        /// </summary>
        Task<int> SaveChangesAsync();

        /// <summary>
        /// Begin a database transaction
        /// </summary>
        Task BeginTransactionAsync();

        /// <summary>
        /// Commit the current transaction
        /// </summary>
        Task CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rollback the current transaction
        /// </summary>
        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}