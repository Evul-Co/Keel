namespace Keel.Infra.Db.Sql.Orm.Transaction;

public interface IDbWrappedTransaction
{
    Task CommitAsync(CancellationToken cancellationToken);
    Task RollbackAsync(CancellationToken cancellationToken);
}