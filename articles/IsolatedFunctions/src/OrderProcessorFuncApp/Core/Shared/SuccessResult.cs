namespace OrderProcessorFuncApp.Core.Shared;

public sealed class SuccessResult : IOperationResult
{
    private SuccessResult() { }

    public static SuccessResult New()
    {
        return new SuccessResult();
    }
}

public sealed class SuccessResult<T> : IOperationResult
{
    private SuccessResult(T result)
    {
        Result = result;
    }

    public static SuccessResult<T> New(T result)
    {
        return new SuccessResult<T>(result);
    }

    public T Result { get; }
}
