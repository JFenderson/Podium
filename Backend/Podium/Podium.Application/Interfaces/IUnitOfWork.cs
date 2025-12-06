using Podium.Core.Entities;

namespace Podium.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<Document> Documents { get; }
    IRepository<DocumentTag> DocumentTags { get; }
    IRepository<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}