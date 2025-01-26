using AutoMapper;
using FluentValidation;
using Invoicing.API.Dto.Result;
using Invoicing.API.Features.Invoices.Shared;
using Invoicing.Infrastructure.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicing.API.Features.Invoices.GetInvoice;

public sealed class GetInvoiceQueryHandler(
    InvoicingDbContext context,
    IValidator<GetInvoiceQuery> validator,
    IMapper mapper
) : IRequestHandler<GetInvoiceQuery, HttpResult<InvoiceResponse>>
{
    public async Task<HttpResult<InvoiceResponse>> Handle(GetInvoiceQuery request, CancellationToken cancellationToken)
    {
        var result = new HttpResult<InvoiceResponse>();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return result.WithValidationErrors(validationResult.Errors);

        var invoice = await context.Invoices
            .Include(i => i.Items.OrderBy(item => item.StartDate).ThenBy(item => item.EndDate))
            .Where(i => i.Month == request.Month && i.Year == request.Year && i.ClientId == request.ClientId)
            .FirstOrDefaultAsync(cancellationToken);
        if (invoice is null)
            return result.WithStatusCode(StatusCodes.Status404NotFound);

        var response = mapper.Map<InvoiceResponse>(invoice);
        return result.WithValue(response);
    }
}