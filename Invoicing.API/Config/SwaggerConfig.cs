namespace Invoicing.API.Config;

public static class SwaggerConfig
{
    public static void UseSwaggerDocumentation(this WebApplication app)
    {
        app.MapOpenApi();
        app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Invoicing API"));
    }
}