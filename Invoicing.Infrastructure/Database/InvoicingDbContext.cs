using Invoicing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Invoicing.Infrastructure.Database;

public class InvoicingDbContext : DbContext
{
    public InvoicingDbContext(DbContextOptions<InvoicingDbContext> options) : base(options)
    {
    }

    public DbSet<Operation> Operations { get; set; }
    public DbSet<Invoice> Invoices { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<Enum>().HaveConversion<string>();
    }
}