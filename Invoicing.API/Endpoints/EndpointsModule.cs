using Invoicing.API.Endpoints.Routes;

namespace Invoicing.API.Endpoints;

public static class EndpointsModule
{
    public static WebApplication MapEndpoints(this WebApplication app)
    {
        return app
            .MapOperationEndpoints()
            .MapInvoiceEndpoints();
    }
}