using Invoicing.API.Extensions;
using Invoicing.API.Mapping;
using Invoicing.API.Mediator;
using Invoicing.API.Middleware;
using Invoicing.Infrastructure.Database;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
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

app.UseMiddleware<ExceptionHandlerMiddleware>();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
await app.RunAsync();