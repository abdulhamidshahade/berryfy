using System.Data;

namespace Berryfy.Domain.Repositories
{
    public interface IUnitOfWork : IAsyncDisposable
    {

        Task BeginTransactionAsync();
        Task<bool> CommitTransactionAsync();
        Task RollbackTransactionAsync();
        ValueTask DisposeAsync();
    }
}
