using Microsoft.EntityFrameworkCore.Storage;
using Podium.Core.Entities;
using Podium.Core.Interfaces;
using Podium.Infrastructure.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Podium.Infrastructure.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;

        // Lazy-loaded repositories
        private IRepository<RefreshToken>? _refreshTokens;
        private IRepository<Student>? _students;
        private IRepository<Guardian>? _guardians;
        private IRepository<BandStaff>? _bandStaff;
        private IRepository<Band>? _bands;
        private IRepository<Video>? _videos;
        private IRepository<ScholarshipOffer>? _scholarshipOffers;
        private IRepository<AuditLog>? _auditLogs;
        private IRepository<BandEvent>? _bandEvents;
        private IRepository<EventRegistration>? _eventRegistrations;
        private IRepository<VideoRating>? _videoRatings;
        private IRepository<Notification>? _notifications;
        private IRepository<StudentInterest>? _studentInterests;
        private IRepository<StudentRating>? _studentRatings;
        private IRepository<StudentGuardian>? _studentGuardian;
        private IRepository<ContactRequest>? _contactRequests;
        private IRepository<ContactLog>? _contactLogs;
        private IRepository<GuardianNotification>? _guardianNotifications;
        private IRepository<GuardianNotificationPreferences>? _guardianNotificationPreferences;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        // Repository properties
        public IRepository<RefreshToken> RefreshTokens =>
            _refreshTokens ??= new Repository<RefreshToken>(_context);

        public IRepository<Student> Students =>
            _students ??= new Repository<Student>(_context);

        public IRepository<Guardian> Guardians =>
            _guardians ??= new Repository<Guardian>(_context);

        public IRepository<BandStaff> BandStaff =>
            _bandStaff ??= new Repository<BandStaff>(_context);

        public IRepository<Band> Bands =>
            _bands ??= new Repository<Band>(_context);

        public IRepository<Video> Videos =>
            _videos ??= new Repository<Video>(_context);
        public IRepository<VideoRating> VideoRatings =>
            _videoRatings ??= new Repository<VideoRating>(_context);

        public IRepository<ScholarshipOffer> ScholarshipOffers =>
            _scholarshipOffers ??= new Repository<ScholarshipOffer>(_context);

        public IRepository<AuditLog> AuditLogs =>
            _auditLogs ??= new Repository<AuditLog>(_context);

        public IRepository<BandEvent> BandEvents =>
            _bandEvents ??= new Repository<BandEvent>(_context);

        public IRepository<EventRegistration> EventRegistrations =>
            _eventRegistrations ??= new Repository<EventRegistration>(_context);
        public IRepository<Notification> Notifications =>
            _notifications ??= new Repository<Notification>(_context);

        public IRepository<StudentInterest> StudentInterests =>
            _studentInterests ??= new Repository<StudentInterest>(_context);

        public IRepository<StudentRating> StudentRatings =>
            _studentRatings ??= new Repository<StudentRating>(_context);

        public IRepository<StudentGuardian> StudentGuardians =>
            _studentGuardian ??= new Repository<StudentGuardian>(_context);

        public IRepository<ContactRequest> ContactRequests => _contactRequests ??= new Repository<ContactRequest>(_context);
        public IRepository<ContactLog> ContactLogs => _contactLogs ??= new Repository<ContactLog>(_context);
        public IRepository<GuardianNotification> GuardianNotifications => _guardianNotifications ??= new Repository<GuardianNotification>(_context);
        public IRepository<GuardianNotificationPreferences> GuardianNotificationPreferences => _guardianNotificationPreferences ??= new Repository<GuardianNotificationPreferences>(_context);

        // Ensure proper disposal and transaction handling
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync(cancellationToken);
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync(cancellationToken);
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}