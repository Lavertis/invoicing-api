﻿namespace Invoicing.API.Middleware;

public sealed class ExceptionHandlerMiddleware(ILogger<ExceptionHandlerMiddleware> logger) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            var response = context.Response;
            response.ContentType = "application/json";
            response.StatusCode = StatusCodes.Status500InternalServerError;
            logger.LogError(exception, "{P0}", exception.Message);
            await response.WriteAsJsonAsync(new { Message = "Internal server error" });
        }
    }
}