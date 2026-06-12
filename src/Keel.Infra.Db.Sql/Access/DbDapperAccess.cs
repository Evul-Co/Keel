using System.Data;
using System.Runtime.CompilerServices;
using Dapper;
using Keel.Infra.Db.Sql.Access.Context;
using Microsoft.EntityFrameworkCore;

namespace Keel.Infra.Db.Sql.Access;

public class DbDapperAccess(IDbSharedContextProvider sharedConnectionProvider)
{
    private async Task<T> ExecuteResilientAsync<T>(Func<Task<T>> operation)
    {
        if (sharedConnectionProvider is IDbLayer dbLayer)
        {
            var strategy = dbLayer.Orm.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(operation);
        }
        return await operation();
    }

    public async Task<T?> ReadOneAsync<T>(string sql, object? param, CancellationToken cancellationToken)
    {
        return await ExecuteResilientAsync(async () =>
        {
            await using var context = await sharedConnectionProvider.GetContextAsync(cancellationToken).ConfigureAwait(false);
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
        });
    }

    public async Task<T?> ReadOneSpAsync<T>(string sql, object? param, CancellationToken cancellationToken)
    {
        return await ExecuteResilientAsync(async () =>
        {
            await using var context = await sharedConnectionProvider.GetContextAsync(cancellationToken).ConfigureAwait(false);
            var connection = context.Connection;

            return await connection.QueryFirstOrDefaultAsync<T>(
                new CommandDefinition(
                    sql,
                    param,
                    commandType: CommandType.StoredProcedure,
                    transaction: context.Transaction,
                    cancellationToken: cancellationToken))
                .ConfigureAwait(false);
        });
    }

    public async Task<IEnumerable<T>> ReadAsync<T>(string sql, object? param, CancellationToken cancellationToken)
    {
        return await ExecuteResilientAsync(async () =>
        {
            await using var context = await sharedConnectionProvider.GetContextAsync(cancellationToken).ConfigureAwait(false);
            var connection = context.Connection;

            return await connection.QueryAsync<T>(
                new CommandDefinition(
                    sql,
                    param,
                    commandType: CommandType.Text,
                    transaction: context.Transaction,
                    cancellationToken: cancellationToken))
                .ConfigureAwait(false);
        });
    }

    public async Task<IEnumerable<T>> ReadSpAsync<T>(string sql, object? param, CancellationToken cancellationToken)
    {
        return await ExecuteResilientAsync(async () =>
        {
            await using var context = await sharedConnectionProvider.GetContextAsync(cancellationToken).ConfigureAwait(false);
            var connection = context.Connection;

            return await connection.QueryAsync<T>(
                new CommandDefinition(
                    sql, 
                    param, 
                    commandType: CommandType.StoredProcedure, 
                    transaction: context.Transaction,
                    cancellationToken: cancellationToken))
                .ConfigureAwait(false);
        });
    }

    public async Task<IEnumerable<T>> ReadSpAsync<T>(string sql, int commandTimeout, object? param, CancellationToken cancellationToken)
    {
        return await ExecuteResilientAsync(async () =>
        {
            await using var context = await sharedConnectionProvider.GetContextAsync(cancellationToken).ConfigureAwait(false);
            var connection = context.Connection;

            return await connection.QueryAsync<T>(
                new CommandDefinition(
                    sql,
                    param,
                    commandType: CommandType.StoredProcedure,
                    transaction: context.Transaction,
                    commandTimeout: commandTimeout,
                    cancellationToken: cancellationToken))
                .ConfigureAwait(false);
        });
    }

    public async Task<IEnumerable<TResult>> QueryAsync<TResult>(Action<DbDapperAccessBuilder> config, CancellationToken cancellationToken)
    {
        return await ExecuteResilientAsync(async () =>
        {
            await using var context = await sharedConnectionProvider.GetContextAsync(cancellationToken).ConfigureAwait(false);
            var connection = context.Connection;

            var builder = new DbDapperAccessBuilder();
            config(builder);

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            }

            return await connection.QueryAsync<TResult>(builder.Build(context.Transaction, cancellationToken)).ConfigureAwait(false);
        });
    }

    public async IAsyncEnumerable<T> StreamAsync<T>(
        string sql, 
        object? param, 
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var context = await sharedConnectionProvider.GetContextAsync(cancellationToken).ConfigureAwait(false);
        var connection = context.Connection;

        var reader = await connection.QueryAsync<T>(
            new CommandDefinition(
                sql, 
                param, 
                transaction: context.Transaction, 
                flags: CommandFlags.None, 
                cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        foreach (var item in reader)
        {
            yield return item;
        }
    }
}