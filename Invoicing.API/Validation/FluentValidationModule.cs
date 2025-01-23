using System.Reflection;
using FluentValidation;

namespace Invoicing.API.Validation;

public static class FluentValidationModule
{
    public static void AddFluentValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
    }
}