using Microsoft.EntityFrameworkCore;

namespace Invoicing.Infrastructure.Database;

public class InvoicingDbContext : DbContext
{
    public InvoicingDbContext(DbContextOptions<InvoicingDbContext> options) : base(options)
    {
    }
}