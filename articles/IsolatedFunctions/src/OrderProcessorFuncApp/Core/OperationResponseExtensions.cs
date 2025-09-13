namespace OrderProcessorFuncApp.Core;

public static class OperationResponseExtensions
{
    public static TData MapSuccess<TData>(this OperationResponse<FailedResult, SuccessResult<TData>> operation)
        where TData : class
    {
        return operation.Result switch
        {
            SuccessResult<TData> success => success.Result,
            _ => throw new InvalidOperationException("Operation did not complete successfully."),
        };
    }
}
