using AutoMapper;
using Invoicing.Domain.Entities;

namespace Invoicing.API.Features.Invoices.Shared
{
    public sealed class InvoiceMappingProfile : Profile
    {
        public InvoiceMappingProfile()
        {
            CreateMap<Invoice, InvoiceResponse>();
            CreateMap<InvoiceItem, InvoiceItemResponse>();
        }
    }
}