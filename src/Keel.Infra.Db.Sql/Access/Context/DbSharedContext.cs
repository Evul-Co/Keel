using System.Data.Common;

namespace Keel.Infra.Db.Sql.Access.Context;

public class DbSharedContext(
    DbConnection connection,
    DbTransaction? transaction,
    bool dedicated) : IDisposable
{
    public DbConnection Connection => connection;
    public DbTransaction? Transaction => transaction;

    public DbCommand CreateCommand()
    {
        if (dedicated)
        {
            connection.Open();
        }

        var command = connection.CreateCommand();
        command.Transaction = transaction;

        return command;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (!dedicated)
        {
            return;
        }

        connection.Dispose();
        transaction?.Dispose();
    }
}