using System.Reflection;
using Invoicing.API.Mediator.Behaviors;

namespace Invoicing.API.Mediator;

public static class MediatorModule
{
    public static void AddMediatorModule(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly());
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
    }
}