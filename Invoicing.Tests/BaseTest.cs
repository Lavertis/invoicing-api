using AutoMapper;
using Invoicing.API.Mapping;
using Invoicing.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Invoicing.Tests;

public class BaseTest : IDisposable, IAsyncDisposable
{
    protected readonly InvoicingDbContext Context;
    protected readonly IMapper Mapper;

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
        GC.SuppressFinalize(this);
        await Context.DisposeAsync();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Context.Dispose();
    }
}