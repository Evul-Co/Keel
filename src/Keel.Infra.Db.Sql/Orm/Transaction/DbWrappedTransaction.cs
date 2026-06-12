using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Keel.Infra.Db.Sql.Orm.Transaction;

internal class DbWrappedTransaction(bool transactionOwner, DatabaseFacade database) 
    : IDbWrappedTransaction
{
    public Task CommitAsync(CancellationToken cancellationToken)
    {
        return transactionOwner ? database.CommitTransactionAsync(cancellationToken) : Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken)
    {
        return transactionOwner ? database.RollbackTransactionAsync(cancellationToken) : Task.CompletedTask;
    }

    public void Dispose()
    {
        if (transactionOwner)
        {
            database.CurrentTransaction?.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (transactionOwner && database.CurrentTransaction != null)
        {
            await database.CurrentTransaction.DisposeAsync().ConfigureAwait(false);
        }
    }
}