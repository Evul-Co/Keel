using Keel.Infra.Db.Sql.Orm.Transaction;
using Microsoft.EntityFrameworkCore;

namespace Keel.Infra.Db.Sql.Orm;

public class BaseDbContext : DbContext, IDbUnitOfWork
{
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    
    public BaseDbContext() { }
    public BaseDbContext(DbContextOptions options) : base(options) { }
    
    public async Task<IDbWrappedTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        await _semaphoreSlim.WaitAsync(cancellationToken);

        try
        {
            if (Database.CurrentTransaction != null)
            {
                return new DbWrappedTransaction(false, Database);
            }
            
            await Database.BeginTransactionAsync(cancellationToken);
            return new DbWrappedTransaction(true, Database);
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }
}