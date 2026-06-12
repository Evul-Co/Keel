using System.Data.Common;
using Keel.Infra.Db.Sql.Access;
using Microsoft.EntityFrameworkCore;

namespace Keel.Infra.Db.Sql;

public interface IDbLayer
{
    DbContext Orm { get; }
    DbDirectAccess Ado { get; }
    DbDapperAccess Dapper { get; }

    Task<DbConnection> GetConnectionAsync(CancellationToken cancellationToken);
    Task<DateTime> GetCurrentUtcDateTimeAsync(CancellationToken cancellationToken);
}

public interface IDbLayer<out TDbContext> : IDbLayer where TDbContext : DbContext
{
    new TDbContext Orm { get; }
}