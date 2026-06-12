using System.Data;
using Dapper;
using Keel.Infra.Db.Sql.Access.Context;

namespace Keel.Infra.Db.Sql.Access;

public class DbDapperAccess(IDbSharedContextProvider sharedConnectionProvider)
{
    public async Task<T?> ReadOneAsync<T>(string sql, object? param, CancellationToken cancellationToken)
    {
        using var context = await sharedConnectionProvider.GetContextAsync(cancellationToken).ConfigureAwait(false);
        var connection = context.Connection;

        return await connection
            .QueryFirstOrDefaultAsync<T>(
                new CommandDefinition(
                    sql,
                    param,
                    commandType: CommandType.Text,
                    transaction: context.Transaction,
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }
    public async Task<T?> ReadOneSpAsync<T>(string sql, object? param, CancellationToken cancellationToken)
    {
        using var context = await sharedConnectionProvider.GetContextAsync(cancellationToken).ConfigureAwait(false);
        var connection = context.Connection;

        return await connection.QueryFirstOrDefaultAsync<T>(
            new CommandDefinition(
                sql,
                param,
                commandType: CommandType.StoredProcedure,
                transaction: context.Transaction,
                cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<T>> ReadAsync<T>(string sql, object? param, CancellationToken cancellationToken)
    {
        using var context = await sharedConnectionProvider.GetContextAsync(cancellationToken).ConfigureAwait(false);
        var connection = context.Connection;

        return await connection.QueryAsync<T>(
            new CommandDefinition(
                sql,
                param,
                commandType: CommandType.Text,
                transaction: context.Transaction,
                cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }
    public async Task<IEnumerable<T>> ReadSpAsync<T>(string sql, object? param, CancellationToken cancellationToken)
    {
        using var context = await sharedConnectionProvider.GetContextAsync(cancellationToken);
        var connection = context.Connection;

        return await connection.QueryAsync<T>(
            sql, 
            param, 
            commandType: CommandType.StoredProcedure, 
            transaction: context.Transaction);
    }
    public async Task<IEnumerable<T>> ReadSpAsync<T>(string sql, int commandTimeout, object? param, CancellationToken cancellationToken)
    {
        using var context = await sharedConnectionProvider.GetContextAsync(cancellationToken);
        var connection = context.Connection;

        return await connection.QueryAsync<T>(
            sql,
            param,
            commandType: CommandType.StoredProcedure,
            transaction: context.Transaction,
            commandTimeout: commandTimeout);
    }

    public async Task<IEnumerable<TResult>> QueryAsync<TResult>(Action<DbDapperAccessBuilder> config, CancellationToken cancellationToken)
    {
        using var context = await sharedConnectionProvider.GetContextAsync(cancellationToken);
        var connection = context.Connection;

        var builder = new DbDapperAccessBuilder();

        config(builder);

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        return await connection.QueryAsync<TResult>(builder.Build(context.Transaction, cancellationToken));
    }
}