using AutoMapper;
using Invoicing.API.Mapping;
using Invoicing.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Invoicing.Tests;

public class BaseTest : IDisposable, IAsyncDisposable
{
    protected readonly InvoicingDbContext Context;
    protected readonly IMapper Mapper;
    private bool _disposed;

    protected BaseTest()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Context = new InvoicingDbContext(options);
        Context.Database.EnsureCreated();
        Mapper = AutoMapperModule.CreateAutoMapper();
    }

    public async ValueTask DisposeAsync()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
        await Context.DisposeAsync();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Context.Dispose();
            }

            _disposed = true;
        }
    }
}