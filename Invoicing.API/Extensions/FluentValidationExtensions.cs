using System.Reflection;
using FluentValidation;

namespace Invoicing.API.Extensions;

public static class FluentValidationExtensions
{
    public static void AddFluentValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
    }
}