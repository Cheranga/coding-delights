using FluentValidation.Results;

namespace OrderProcessorFuncApp.Core;

public sealed class FailedResult : IOperationResult
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
