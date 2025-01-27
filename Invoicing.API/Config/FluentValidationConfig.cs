using System.Reflection;
using FluentValidation;

namespace Invoicing.API.Config;

public static class FluentValidationConfig
{
    public static void AddFluentValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
    }
}