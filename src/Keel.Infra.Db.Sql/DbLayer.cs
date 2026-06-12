using System.Data;
using System.Data.Common;
using Keel.Infra.Db.Sql.Access;
using Keel.Infra.Db.Sql.Access.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Keel.Infra.Db.Sql;

public abstract class DbLayer : IDbLayer, IDbSharedContextProvider
{
    private readonly Lazy<DbContext> _lazyOrm;
    private readonly Lazy<DbDirectAccess> _lazyAdo;
    private readonly Lazy<DbDapperAccess> _lazyDapper;

    protected DbLayer()
    {
        _lazyOrm = new Lazy<DbContext>(InternalCreateDbContext);

        _lazyAdo = new Lazy<DbDirectAccess>(InternalCreateDirectAccess);

        _lazyDapper = new Lazy<DbDapperAccess>(
            () => new DbDapperAccess(this));
    }

    public DbContext Orm => _lazyOrm.Value;
    public DbDirectAccess Ado => _lazyAdo.Value;
    public DbDapperAccess Dapper => _lazyDapper.Value;

    public async Task<DbConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        var context = await ((IDbSharedContextProvider)this)
            .GetContextAsync(cancellationToken)
            .ConfigureAwait(false);

        return context.Connection;
    }
    public Task<DateTime> GetCurrentUtcDateTimeAsync(CancellationToken cancellationToken)
    {
        return Ado.GetCurrentUtcDateTimeAsync(cancellationToken);
    }
    
    protected abstract DbContext InternalCreateDbContext();
    protected abstract DbDirectAccess InternalCreateDirectAccess();

    async Task<DbSharedContext> IDbSharedContextProvider.GetContextAsync(CancellationToken cancellationToken)
    {
        var transaction = Orm.Database.CurrentTransaction?.GetDbTransaction();

        var connection = Orm.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        return new DbSharedContext(
            connection,
            transaction,
            false);
    }

    async Task<DbCommand> IDbSharedContextProvider.GetCommandAsync(CancellationToken cancellationToken)
    {
        var connection = Orm.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        return connection.CreateCommand();
    }
}

public abstract class DbLayer<TDbContext>(TDbContext context) : DbLayer, IDbLayer<TDbContext>
    where TDbContext : DbContext
{
    public new TDbContext Orm => context;

    protected override DbContext InternalCreateDbContext() => Orm;
}

