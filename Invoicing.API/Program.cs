using Invoicing.API.CQRS;
using Invoicing.API.Mapping;
using Invoicing.API.Middleware;
using Invoicing.API.Validation;
using Invoicing.Infrastructure.Database;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDatabaseModule(builder.Configuration);
builder.Services.AddMiddlewareModule();
builder.Services.AddAutoMapperModule();
builder.Services.AddFluentValidators();
builder.Services.AddMediatorModule();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Invoicing API"));
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();