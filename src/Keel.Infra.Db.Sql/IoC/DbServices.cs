using Keel.Infra.Db.Sql;
using Keel.Infra.Db.Sql.Orm;
using Keel.Infra.Db.Sql.Orm.Transaction;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore;

public static class DbServices
{
    public static IServiceCollection EnableDbLayer<TDbContext, TDbLayer>(this IServiceCollection services) 
        where TDbContext : BaseDbContext where TDbLayer : class, IDbLayer<TDbContext>
    {
        services
            .AddScoped<IDbLayer>(
                provider => provider.GetRequiredService<IDbLayer<TDbContext>>())
            .AddScoped<IDbLayer<TDbContext>, TDbLayer>()
            .AddScoped<IDbUnitOfWork, TDbContext>();

        return services;
    }
}