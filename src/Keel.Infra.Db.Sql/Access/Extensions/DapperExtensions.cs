using System.Data;
using Dapper;

// ReSharper disable CheckNamespace
#pragma warning disable S3903 // Types should be defined in named namespaces
public static class DapperExtensions
#pragma warning restore S3903 // Types should be defined in named namespaces
// ReSharper restore CheckNamespace
{
    public static async Task<IEnumerable<TParent>> QueryParentChildAsync<TParent, TChild, TParentKey>(
        this IDbConnection connection,
        string sql,
        Func<TParent, TParentKey> parentKeySelector,
        Func<TParent, IList<TChild>> childSelector,
        dynamic? param = null, IDbTransaction? transaction = null, bool buffered = true, string splitOn = "ID", int? commandTimeout = null, CommandType? commandType = null) 
        where TParentKey : notnull
    {
        var cache = new Dictionary<TParentKey, TParent>();

        await connection.QueryAsync<TParent, TChild, TParent>(
            sql,
            (parent, child) =>
                {
                    var key = parentKeySelector(parent);

                    cache.TryAdd(key, parent);

                    var cachedParent = cache[key];

                    var children = childSelector(cachedParent);
                    children.Add(child);

                    return cachedParent;
                },
            param as object, 
            transaction, 
            buffered, 
            splitOn, 
            commandTimeout, 
            commandType);

        return cache.Values;
    }
}