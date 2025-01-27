using Microsoft.EntityFrameworkCore;

namespace Invoicing.API.Extensions
{
    public static class MigrationExtensions
    {
        public static async Task ApplyMigrationsAsync<TContext>(this IApplicationBuilder app) where TContext : DbContext
        {
            using var scope = app.ApplicationServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
            await dbContext.Database.MigrateAsync();
        }
    }
}