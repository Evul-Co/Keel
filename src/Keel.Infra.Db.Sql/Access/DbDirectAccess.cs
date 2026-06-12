using System.Data;
using System.Data.Common;
using Keel.Infra.Db.Sql.Access.Context;
using Keel.Infra.Db.Sql.Exceptions;

namespace Keel.Infra.Db.Sql.Access;

public abstract class DbDirectAccess(IDbSharedContextProvider provider)
{
    public IDbSharedContextProvider Provider => provider;
    
    public async Task<DataSet> DataSetAsync(
        string command, CommandType commandType, CancellationToken cancellationToken, params DbParameter[] parameters)
    {
        await using var comm = await provider
            .GetCommandAsync(cancellationToken)
            .ConfigureAwait(false);

        comm.CommandText = command;
        comm.CommandType = commandType;
        comm.CommandTimeout = 200;

        comm.Parameters.AddRange(parameters);

        var set = new DataSet();
        
        using var adapter = InternalCreateDataAdapter(comm);
        adapter.Fill(set);

        return set;
    }
    public async Task<DataTable> DataTableAsync(
        string command, CommandType commandType, CancellationToken cancellationToken, params DbParameter[] parameters)
    {
        await using var comm = await provider.GetCommandAsync(cancellationToken);

        comm.CommandText = command;
        comm.CommandType = commandType;
        comm.CommandTimeout = 200;

        comm.Parameters.AddRange(parameters);

        var table = new DataTable();

        using var adapter = InternalCreateDataAdapter(comm);
        adapter.Fill(table);

        return table;
    }
    
    public async Task<DataRow?> DataRowAsync(
        string command, CommandType commandType, CancellationToken cancellationToken, params DbParameter[] parameters)
    {
        await using var comm = await provider.GetCommandAsync(cancellationToken);

        comm.CommandText = command;
        comm.CommandType = commandType;
        comm.CommandTimeout = 200;

        comm.Parameters.AddRange(parameters);

        var table = new DataTable();

        using var adapter = InternalCreateDataAdapter(comm);
        adapter.Fill(table);

        return table
            .AsEnumerable()
            .FirstOrDefault();
    }

    public async Task<TScalar?> ScalarAsync<TScalar>(
        string command, CommandType commandType, CancellationToken cancellationToken, params DbParameter[] parameters)
    {
        await using var comm = await provider.GetCommandAsync(cancellationToken);

        comm.CommandText = command;
        comm.CommandType = commandType;
        comm.CommandTimeout = 200;

        comm.Parameters.AddRange(parameters);

        return (TScalar?)await comm.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> NonQueryAsync(
        string command, CommandType commandType, CancellationToken cancellationToken, params DbParameter[] parameters)
    {
        await using var comm = await provider.GetCommandAsync(cancellationToken);

        comm.CommandText = command;
        comm.CommandType = commandType;
        comm.CommandTimeout = 200;

        comm.Parameters.AddRange(parameters);

        return await comm.ExecuteNonQueryAsync(cancellationToken);
    }

    

    public void Read(
        string command, CommandType commandType, Action<DbDataReader> callback, CancellationToken cancellationToken, params DbParameter[] parameters)
    {
        using var comm = provider.GetCommand();

        comm.CommandText = command;
        comm.CommandType = commandType;
        comm.CommandTimeout = 200;

        comm.Parameters.AddRange(parameters);

        using var reader = comm.ExecuteReader();
        while (reader.NextResult())
        {
            callback(reader);
        }
    }

    public IEnumerable<T> Read<T>(
        string command, CommandType commandType, Func<DbDataReader, T> processAction, CancellationToken cancellationToken,
        params DbParameter[] parameters)
    {
        using var comm = provider.GetCommand();

        comm.CommandText = command;
        comm.CommandType = commandType;
        comm.CommandTimeout = 200;

        comm.Parameters.AddRange(parameters);

        using var reader = comm.ExecuteReader();
        while (reader.Read())
        {
            yield return processAction(reader);
        }
    }

    public async Task ReadAsync(
        string command, CommandType commandType, Action<DbDataReader> callback, CancellationToken cancellationToken, params DbParameter[] parameters)
    {
        await using var comm = await provider.GetCommandAsync(cancellationToken).ConfigureAwait(false);

        comm.CommandText = command;
        comm.CommandType = commandType;
        comm.CommandTimeout = 200;

        comm.Parameters.AddRange(parameters);

        using var reader = await comm.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.NextResultAsync(cancellationToken).ConfigureAwait(false))
        {
            callback(reader);
        }
    }

    public async IAsyncEnumerable<T> ReadAsync<T>(
        string command, CommandType commandType, Func<DbDataReader, T> processAction, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken,
        params DbParameter[] parameters)
    {
        await using var comm = await provider.GetCommandAsync(cancellationToken).ConfigureAwait(false);

        comm.CommandText = command;
        comm.CommandType = commandType;
        comm.CommandTimeout = 200;

        comm.Parameters.AddRange(parameters);

        using var reader = await comm.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return processAction(reader);
        }
    }
    
    public async Task<TResult> QueryAsync<TResult>(Action<DbDirectAccessBuilder> config, CancellationToken cancellationToken)
    {
        var context = await provider.GetContextAsync(cancellationToken).ConfigureAwait(false);
        await using var comm = context.CreateCommand();

        var builder = new DbDirectAccessBuilder(this, comm);
        
        config(builder);

        builder.SetExecutionByReturnType<TResult>();
        if (builder.Mode == DbDirectAccessBuilder.EExecMode.PrimitiveValue)
        {
            var scalarResult = await comm.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return CastScalar<TResult>(scalarResult);
        }

        var set = new DataSet();

        using var adapter = InternalCreateDataAdapter(comm);

        await Task.Run(() => adapter.Fill(set), cancellationToken);

        if (builder.Mode == DbDirectAccessBuilder.EExecMode.DataRow)
        {
            if (set.Tables.Count < 1 || set.Tables[0].Rows.Count < 1)
            {
                throw XFlowException.Create("Query don't return valida result");
            }

            return (TResult)(object)set.Tables[0].Rows[0];
        }

        if (builder.Mode == DbDirectAccessBuilder.EExecMode.DataTable)
        {
            if (set.Tables.Count < 1)
            {
                throw XFlowException.Create("Query don't return valida result");
            }

            return (TResult)(object)set.Tables[0];
        }

        return (TResult)(object)set;
    }

    public async Task<TResult> NonQueryAsync<TResult>(Action<DbDirectAccessBuilder> config, CancellationToken cancellationToken)
    {
        var context = await provider.GetContextAsync(cancellationToken).ConfigureAwait(false);
        await using var command = context.CreateCommand();

        var builder = new DbDirectAccessBuilder(this, command);
        config(builder);

        builder.SetExecutionByReturnType<TResult>();
        if (builder.Mode == DbDirectAccessBuilder.EExecMode.PrimitiveValue)
        {
            var scalarResult = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return CastScalar<TResult>(scalarResult);
        }

        var set = new DataSet();

        using var adapter = InternalCreateDataAdapter(command);

        await Task.Run(() => adapter.Fill(set), cancellationToken);

        if (builder.Mode == DbDirectAccessBuilder.EExecMode.DataRow)
        {
            if (set.Tables.Count < 1 || set.Tables[0].Rows.Count < 1)
            {
                throw XFlowException.Create("Query don't return valida result");
            }

            return (TResult)(object)set.Tables[0].Rows[0];
        }

        if (builder.Mode == DbDirectAccessBuilder.EExecMode.DataTable)
        {
            if (set.Tables.Count < 1)
            {
                throw XFlowException.Create("Query don't return valida result");
            }

            return (TResult)(object)set.Tables[0];
        }

        return (TResult)(object)set;
    }

    private static TResult CastScalar<TResult>(object? value)
    {
        if (value == null || value == DBNull.Value)
        {
            return default!;
        }

        var targetType = typeof(TResult);
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        return (TResult)Convert.ChangeType(value, underlyingType);
    }

    public DbParameter CreateParameter(string name, DbType dbType, object? value)
    {
        return InternalCreateParameter(name, dbType, value);
    }

    public async Task<DateTime> GetCurrentUtcDateTimeAsync(CancellationToken cancellationToken)
    {
        var dt = await ScalarAsync<DateTime>(InternalGetCurrentUtcDateTimeSql(), CommandType.Text, cancellationToken);

        return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
    }

    protected abstract string InternalGetCurrentUtcDateTimeSql();
    protected abstract DbDataAdapter InternalCreateDataAdapter(DbCommand comm);
    protected abstract DbParameter InternalCreateParameter(string name, DbType dbType, object? value);
}