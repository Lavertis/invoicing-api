using Carter;
using Invoicing.API.Config;
using Invoicing.API.Extensions;
using Invoicing.API.Mediator;
using Invoicing.API.Middleware;
using Invoicing.Infrastructure.Database;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDatabaseModule(builder.Configuration);
builder.Services.AddMiddlewareModule();
builder.Services.AddMapsterModule();
builder.Services.AddFluentValidators();
builder.Services.AddMediatorModule();
builder.Services.AddCarter();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerDocumentation();
    await app.ApplyMigrationsAsync<ApplicationDbContext>();
}

app.UseHttpsRedirection();
app.UseMiddleware<ExceptionHandlerMiddleware>();
app.MapCarter();
await app.RunAsync();