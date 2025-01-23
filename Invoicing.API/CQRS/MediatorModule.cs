namespace Invoicing.API.CQRS;

public static class MediatorModule
{
    public static void AddMediatorModule(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies()));
    }
}