using System.Reflection;
using Invoicing.Infrastructure.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Invoicing.Infrastructure.Database;

public static class DatabaseModule
{
    public static void AddDatabaseModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(
            GetConnectionString(configuration),
            x => x.MigrationsAssembly(Assembly.GetExecutingAssembly())
        ));
    }

    private static string GetConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");
        if (connectionString == null)
            throw new ConfigurationException("Cannot get Database connection string from configuration.");
        return connectionString;
    }
}