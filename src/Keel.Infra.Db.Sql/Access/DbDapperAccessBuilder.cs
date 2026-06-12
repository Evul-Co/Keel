using System.Data;
using System.Data.Common;
using Dapper;

namespace Keel.Infra.Db.Sql.Access;

public class DbDapperAccessBuilder
{
    private readonly DynamicParameters _parameters = new();

    private int? _commandTimeout;
    private string _commandText = null!;
    private CommandType _commandType;

    public DbDapperAccessBuilder ForCommand(string command, CommandType commandType)
    {
        _commandText = command;
        _commandType = commandType;

        return this;
    }

    public DbDapperAccessBuilder ForText(string command)
    {
        _commandText = command;
        _commandType = CommandType.Text;

        return this;
    }

    public DbDapperAccessBuilder ForStoredProc(string command)
    {
        _commandText = command;
        _commandType = CommandType.StoredProcedure;

        return this;
    }

    public DbDapperAccessBuilder WithTimeout(int timeout)
    {
        _commandTimeout = timeout;

        return this;
    }

    public DbDapperAccessBuilder AddInt(string name, int? value)
    {
        _parameters.Add(name, value, DbType.Int32);

        return this;
    }

    public DbDapperAccessBuilder AddDateTime(string name, DateTime value)
    {
        _parameters.Add(name, value, DbType.DateTime);

        return this;
    }

    public DbDapperAccessBuilder AddString(string name, string value)
    {
        _parameters.Add(name, value, DbType.String);

        return this;
    }

    public CommandDefinition Build(DbTransaction? transaction, CancellationToken cancellationToken)
    {
        return new CommandDefinition(
            _commandText,
            _parameters,
            transaction,
            _commandTimeout,
            _commandType,
            cancellationToken: cancellationToken);
    }
}