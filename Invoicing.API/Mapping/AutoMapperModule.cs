using AutoMapper;
using Invoicing.API.Mapping.Profiles;

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
        var mapperConfiguration = new MapperConfiguration(options => { options.AddProfile<InvoiceMappingProfile>(); });
        var mapper = mapperConfiguration.CreateMapper();
        return mapper;
    }
}