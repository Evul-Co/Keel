namespace Keel.Infra.Db.Sql.Exceptions;

public class XFlowException : ApplicationException
{
    private XFlowException(string message) : base(message)
    {
    }

    private XFlowException(string message, Exception inner) : base(message, inner)
    {
    }
    
    public static Exception Create(string message)
    {
        return new XFlowException(message);
    }
}