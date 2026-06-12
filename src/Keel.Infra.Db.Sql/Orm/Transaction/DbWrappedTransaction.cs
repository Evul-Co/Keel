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
}