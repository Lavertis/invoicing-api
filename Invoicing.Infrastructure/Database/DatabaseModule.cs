using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Invoicing.Infrastructure.Database;

public static class DatabaseModule
{
    public static void AddDatabaseModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<InvoicingDbContext>(options => options.UseSqlite(
            GetConnectionString(configuration),
            x => x.MigrationsAssembly(Assembly.GetExecutingAssembly())
        ));
    }

    private static string GetConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("InvoicingDB");
        if (connectionString == null)
            throw new Exception("Cannot get InvoicingDB connection string");
        return connectionString;
    }
}