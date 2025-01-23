namespace Invoicing.API.Dto.Result;

public abstract class Result<TResult, TValue> where TResult : Result<TResult, TValue>
{
    public Error? Error { get; private set; }
    public TValue? Value { get; private set; }

    public bool IsError => Error != null;

    public TResult WithValue(TValue value)
    {
        Value = value;
        return (TResult)this;
    }

    public TResult WithError(Error? error)
    {
        Error = error;
        return (TResult)this;
    }
}