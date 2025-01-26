namespace Invoicing.API.Extensions;

public static class SwaggerExtensions
{
    public static void UseSwaggerDocumentation(this WebApplication app)
    {
        app.MapOpenApi();
        app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Invoicing API"));
    }
}