using System.Reflection;
using Mapster;

namespace Invoicing.API.Config;

public static class MapsterConfig
{
    public static void AddMapsterModule(this IServiceCollection services)
    {
        TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
    }
}