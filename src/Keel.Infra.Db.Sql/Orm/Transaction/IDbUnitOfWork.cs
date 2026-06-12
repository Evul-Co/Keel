namespace Keel.Infra.Db.Sql.Orm.Transaction;

public interface IDbUnitOfWork
{
    public Task<IDbWrappedTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
}