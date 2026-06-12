namespace Keel.Infra.Db.Sql.Orm.Transaction;

public interface IDbWrappedTransaction : IDisposable, IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
    Task RollbackAsync(CancellationToken cancellationToken);
}