using Invoicing.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Invoicing.Tests;

public class BaseTest : IDisposable, IAsyncDisposable
{
    protected readonly ApplicationDbContext Context;
    private bool _disposed;

    protected BaseTest()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Context = new ApplicationDbContext(options);
        Context.Database.EnsureCreated();
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (!_disposed)
        {
            await Context.DisposeAsync();
            _disposed = true;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing) Context.Dispose();
        _disposed = true;
    }

    ~BaseTest()
    {
        Dispose(false);
    }
}