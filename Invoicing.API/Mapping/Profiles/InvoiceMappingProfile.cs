using AutoMapper;
using Invoicing.API.Features.Invoices.Shared;
using Invoicing.Domain.Entities;

namespace Invoicing.API.Mapping.Profiles
{
    public class InvoiceMappingProfile : Profile
    {
        public InvoiceMappingProfile()
        {
            CreateMap<Invoice, InvoiceResponse>();
            CreateMap<InvoiceItem, InvoiceItemResponse>();
        }
    }
}