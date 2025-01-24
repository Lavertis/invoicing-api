namespace Invoicing.API.Dto.Common;

public class IdResponse<T>
{
    public IdResponse(T id)
    {
        Id = id;
    }

    public T Id { get; }
}