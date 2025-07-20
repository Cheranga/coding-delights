namespace OrderProcessorFuncApp.Core;

public sealed class OperationResponse<TA, TB>
    where TA : IOperationResult
    where TB : IOperationResult
{
    public IOperationResult Result { get; }

    private OperationResponse(IOperationResult result)
    {
        Result = result;
    }

    public static implicit operator OperationResponse<TA, TB>(TA a)
    {
        return new OperationResponse<TA, TB>(a);
    }

    public static implicit operator OperationResponse<TA, TB>(TB b)
    {
        return new OperationResponse<TA, TB>(b);
    }
}
