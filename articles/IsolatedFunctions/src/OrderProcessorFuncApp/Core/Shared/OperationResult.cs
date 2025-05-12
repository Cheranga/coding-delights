using FluentValidation.Results;

namespace OrderProcessorFuncApp.Core.Shared;

public abstract class OperationResult
{
    public sealed class FailedResult : OperationResult
    {
        private FailedResult(string errorCode, string errorMessage)
        {
            Error = ErrorResponse.New(errorCode, errorMessage);
        }

        private FailedResult(string errorCode, string errorMessage, ValidationResult validationResult)
        {
            Error = ErrorResponse.New(errorCode, errorMessage, validationResult);
        }

        public static FailedResult New(string errorCode, string errorMessage)
        {
            return new FailedResult(errorCode, errorMessage);
        }

        public static FailedResult New(string errorCode, string errorMessage, ValidationResult validationResult)
        {
            return new FailedResult(errorCode, errorMessage, validationResult);
        }

        public ErrorResponse Error { get; }
    }

    public sealed class SuccessResult<T> : OperationResult
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
}
