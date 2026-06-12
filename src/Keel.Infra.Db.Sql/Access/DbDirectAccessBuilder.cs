using System.Data;
using System.Data.Common;

namespace Keel.Infra.Db.Sql.Access;

public class DbDirectAccessBuilder(DbDirectAccess access, DbCommand cmd)
{
    public enum EExecMode
    {
        DataRow,
        DataTable,
        DataSet,
        PrimitiveValue
    }

    public EExecMode Mode { get; private set; } = EExecMode.DataTable;

    public DbDirectAccessBuilder SetExecutionByReturnType<TResult>()
    {
        Mode = IdentifyExecMode<TResult>();

        return this;
    }

    public DbDirectAccessBuilder SetExecutionMode(EExecMode mode)
    {
        Mode = mode;

        return this;
    }

    public DbDirectAccessBuilder ForCommand(string command, CommandType commandType)
    {
        cmd.CommandText = command;
        cmd.CommandType = commandType;

        return this;
    }

    public DbDirectAccessBuilder ForText(string command)
    {
        cmd.CommandText = command;
        cmd.CommandType = CommandType.Text;

        return this;
    }

    public DbDirectAccessBuilder ForStoredProc(string command)
    {
        cmd.CommandText = command;
        cmd.CommandType = CommandType.StoredProcedure;

        return this;
    }

    public DbDirectAccessBuilder WithTimeout(int timeout)
    {
        cmd.CommandTimeout = timeout;

        return this;
    }

    public DbDirectAccessBuilder AddByte(string name, byte value)
    {
        cmd.Parameters.Add(
            access.CreateParameter(name, DbType.Byte, value));

        return this;
    }

    public DbDirectAccessBuilder AddInt(string name, int? value)
    {
        cmd.Parameters.Add(
            access.CreateParameter(name, DbType.Int32, value));

        return this;
    }

    public DbDirectAccessBuilder AddEnum(string name, Enum value)
    {
        cmd.Parameters.Add(
            access.CreateParameter(name, DbType.Int32, value));

        return this;
    }

    public DbDirectAccessBuilder AddString(string name, string value)
    {
        cmd.Parameters.Add(
            access.CreateParameter(name, DbType.String, value));

        return this;
    }

    public DbDirectAccessBuilder AddParameters(params DbParameter[] parameters)
    {
        cmd.Parameters.AddRange(parameters);

        return this;
    }

    private static EExecMode IdentifyExecMode<TResult>()
    {
        var type = typeof(TResult);
        if (type == typeof(DataRow))
        {
            return EExecMode.DataRow;
        }

        if (type == typeof(DataTable))
        {
            return EExecMode.DataTable;
        }

        if (type == typeof(DataSet))
        {
            return EExecMode.DataSet;
        }

        return EExecMode.PrimitiveValue;
    }
}