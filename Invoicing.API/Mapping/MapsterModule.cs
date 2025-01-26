using System.Reflection;
using Mapster;

namespace Invoicing.API.Mapping;

public static class MapsterModule
{
    public static void AddMapsterModule(this IServiceCollection services)
    {
        TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
    }
}