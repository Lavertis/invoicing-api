﻿namespace Invoicing.API.Dto.Result;

public abstract class Result<TResult, TValue> where TResult : Result<TResult, TValue>
{
    public string? ErrorMessage { get; private set; }
    public TValue? Data { get; private set; }

    public bool IsError { get; private set; }
    public bool IsSuccess => !IsError;

    public TResult WithData(TValue data)
    {
        Data = data;
        return (TResult)this;
    }

    public TResult WithError(string? errorMessage)
    {
        ErrorMessage = errorMessage;
        IsError = true;
        return (TResult)this;
    }
}