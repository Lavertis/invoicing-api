using AutoMapper;
using Invoicing.API.Features.Invoices.Shared;
using Invoicing.Domain.Entities;

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
        var mapperConfiguration = new MapperConfiguration(options =>
        {
            options.CreateMap<Invoice, InvoiceResponse>();
            options.CreateMap<InvoiceItem, InvoiceItemResponse>();
        });
        var mapper = mapperConfiguration.CreateMapper();
        return mapper;
    }
}