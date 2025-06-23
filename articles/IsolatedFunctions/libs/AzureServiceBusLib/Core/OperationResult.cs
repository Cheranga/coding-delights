namespace AzureServiceBusLib.Core;

/// <summary>
/// Represents the result of an operation that can either succeed or fail.
/// </summary>
public abstract record OperationResult
{
    /// <summary>
    /// Creates a failed operation result with error details.
    /// </summary>
    /// <param name="errorCode">The error code identifying the failure type.</param>
    /// <param name="errorMessage">The human-readable error message.</param>
    /// <param name="exception">Optional exception that caused the failure.</param>
    /// <returns>A new instance of <see cref="FailedResult"/>.</returns>
    public static FailedResult Failure(string errorCode, string errorMessage, Exception? exception = null) =>
        new(errorCode, errorMessage, exception);

    /// <summary>
    /// Creates a successful operation result without a return value.
    /// </summary>
    /// <returns>A new instance of <see cref="SuccessResult"/>.</returns>
    public static SuccessResult Success() => new();

    /// <summary>
    /// Creates a successful operation result containing a value.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="result">The value to include in the result.</param>
    /// <returns>A new instance of <see cref="SuccessResult{T}"/>.</returns>
    public static SuccessResult<T> Success<T>(T result) => new(result);

    /// <summary>
    /// Represents a failed operation result with detailed error information.
    /// </summary>
    public sealed record FailedResult : OperationResult
    {
        /// <summary>
        /// Gets the error code identifying the failure type.
        /// </summary>
        public string ErrorCode { get; }

        /// <summary>
        /// Gets the human-readable error message.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Gets the optional exception that caused the failure.
        /// </summary>
        public Exception? Exception { get; }

        internal FailedResult(string errorCode, string errorMessage, Exception? exception = null)
        {
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
            Exception = exception;
        }
    }

    /// <summary>
    /// Represents a successful operation result without a return value.
    /// </summary>
    public sealed record SuccessResult : OperationResult
    {
        internal SuccessResult() { }
    }

    /// <summary>
    /// Represents a successful operation result containing a value.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    public sealed record SuccessResult<T> : OperationResult
    {
        internal SuccessResult(T result)
        {
            Result = result;
        }

        /// <summary>
        /// Gets the result value of the successful operation.
        /// </summary>
        public T Result { get; }
    }
}
