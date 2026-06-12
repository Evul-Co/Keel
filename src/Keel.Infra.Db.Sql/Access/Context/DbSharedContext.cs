using System.Data.Common;

namespace Keel.Infra.Db.Sql.Access.Context;

public class DbSharedContext(
    DbConnection connection,
    DbTransaction? transaction,
    bool dedicated) : IDisposable, IAsyncDisposable
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

    public async Task<DbCommand> CreateCommandAsync(CancellationToken cancellationToken = default)
    {
        if (dedicated)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
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

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        if (!dedicated)
        {
            return;
        }

        await connection.DisposeAsync().ConfigureAwait(false);
        if (transaction != null)
        {
            await transaction.DisposeAsync().ConfigureAwait(false);
        }
    }
}