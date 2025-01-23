namespace Invoicing.API.Middleware;

public static class MiddlewareModule
{
    public static void AddMiddlewareModule(this IServiceCollection services)
    {
        services.AddScoped<ExceptionHandlerMiddleware>();
    }
}