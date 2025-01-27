using Invoicing.API.Endpoints;
using Invoicing.API.Extensions;
using Invoicing.API.Mapping;
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerDocumentation();
}

app.UseHttpsRedirection();
app.UseMiddleware<ExceptionHandlerMiddleware>();
app.MapEndpoints();
await app.RunAsync();