using System.Data.Common;

namespace Keel.Infra.Db.Sql.Access.Context;

public interface IDbSharedContextProvider
{
    Task<DbSharedContext> GetContextAsync(CancellationToken cancellationToken);
    Task<DbCommand> GetCommandAsync(CancellationToken cancellationToken);

    DbSharedContext GetContext();
    DbCommand GetCommand();
}