using AutoMapper;

namespace Invoicing.API.Mapping;

public static class AutoMapperModule
{
    public static void AddAutoMapperModule(this IServiceCollection services)
    {
        var mapper = CreateAutoMapper();
        services.AddSingleton(mapper);
    }

    public static IMapper CreateAutoMapper()
    {
        var mapperConfiguration = new MapperConfiguration(options => { });
        var mapper = mapperConfiguration.CreateMapper();
        return mapper;
    }
}