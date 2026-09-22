using Npgsql;
using System.Data;

namespace Berryfy.Domain.Repositories
{
    public class UnitOfWork : IUnitOfWork, IAsyncDisposable
    {
        private readonly NpgsqlConnection _connection;
        private NpgsqlTransaction _currentTransaction;

        public UnitOfWork(NpgsqlConnection connection)
        {
            _connection = connection;
        }

        public async Task BeginTransactionAsync()
        {
            if (_currentTransaction != null)
            {
                return;
            }

            // Ensure the connection is open before starting a transaction
            if (_connection.State != ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }

            _currentTransaction = await _connection.BeginTransactionAsync();
        }

        public async Task<bool> CommitTransactionAsync()
        {
            if (_currentTransaction == null)
            {
                return false;
            }

            try
            {
                await _currentTransaction.CommitAsync();
                return true;
            }
            finally
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_currentTransaction == null)
            {
                return;
            }

            try
            {
                await _currentTransaction.RollbackAsync();
            }
            finally
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }

            if (_connection != null && _connection.State != ConnectionState.Closed)
            {
                await _connection.DisposeAsync();
            }
        }
    }
}
