using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Invoicing.Infrastructure.Database;

public static class DatabaseModule
{
    public static void AddDatabaseModule(this IServiceCollection services)
    {
        services.AddDbContext<InvoicingDbContext>(options => options.UseInMemoryDatabase("InvoicingDB"));
    }
}