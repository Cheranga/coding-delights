namespace OrderPublisher.Console.Core;

public abstract record OperationResult
{
    public static FailedResult Failure(string errorCode, string errorMessage, Exception? exception = null) =>
        new(errorCode, errorMessage, exception);

    public static SuccessResult Success() => new();

    public static SuccessResult<T> Success<T>(T result) => new(result);

    public sealed record FailedResult : OperationResult
    {
        public string ErrorCode { get; }
        public string ErrorMessage { get; }
        public Exception? Exception { get; }

        internal FailedResult(string errorCode, string errorMessage, Exception? exception = null)
        {
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
            Exception = exception;
        }
    }

    public sealed record SuccessResult : OperationResult
    {
        internal SuccessResult() { }
    }

    public sealed record SuccessResult<T> : OperationResult
    {
        internal SuccessResult(T result)
        {
            Result = result;
        }

        public T Result { get; }
    }
}
