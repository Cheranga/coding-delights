namespace OrderProcessorFuncApp.Core.Shared;

public sealed class OperationResponse<TA, TB>
    where TA : OperationResult
    where TB : OperationResult
{
    public OperationResult Result { get; }

    private OperationResponse(OperationResult result)
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
