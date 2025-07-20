namespace OrderProcessorFuncApp.Core;

public static class OperationResponseExtensions
{
    public static TResult Match<TA, TB, TResult>(
        this OperationResponse<TA, TB> response,
        Func<TA, TResult> onFirst,
        Func<TB, TResult> onSecond
    )
        where TA : IOperationResult
        where TB : IOperationResult
    {
        return response.Result switch
        {
            TA a => onFirst(a),
            TB b => onSecond(b),
            _ => throw new InvalidOperationException("Unknown operation result type."),
        };
    }

    public static Task<TResult> Match<TA, TB, TResult>(
        this OperationResponse<TA, TB> response,
        Func<TA, Task<TResult>> onFirst,
        Func<TB, Task<TResult>> onSecond
    )
        where TA : IOperationResult
        where TB : IOperationResult
    {
        return response.Result switch
        {
            TA a => onFirst(a),
            TB b => onSecond(b),
            _ => throw new InvalidOperationException("Unknown operation result type."),
        };
    }

    public static void Match<TA, TB>(this OperationResponse<TA, TB> response, Action<TA> whenFirst, Action<TB> whenSecond)
        where TA : IOperationResult
        where TB : IOperationResult
    {
        switch (response.Result)
        {
            case TA a:
                whenFirst(a);
                break;
            case TB b:
                whenSecond(b);
                break;
            default:
                throw new InvalidOperationException($"Unexpected result type: {response.Result.GetType()}");
        }
    }

    public static Task Match<TA, TB>(this OperationResponse<TA, TB> response, Func<TA, Task> whenFirst, Func<TB, Task> whenSecond)
        where TA : IOperationResult
        where TB : IOperationResult
    {
        return response.Result switch
        {
            TA a => whenFirst(a),
            TB b => whenSecond(b),
            _ => throw new InvalidOperationException($"Unexpected result type: {response.Result.GetType()}"),
        };
    }
}
