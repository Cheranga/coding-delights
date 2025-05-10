using FluentValidation.Results;

namespace OrderProcessorFuncApp.Core.Shared;

public sealed record ErrorResponse
{
    private ErrorResponse(string errorCode, string errorMessage, IEnumerable<ErrorResponse>? errorDetails)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ErrorDetails = errorDetails?.ToList();
    }

    public string ErrorMessage { get; init; }

    public string ErrorCode { get; init; }

    private IReadOnlyCollection<ErrorResponse>? ErrorDetails { get; init; }

    public static ErrorResponse New(string errorCode, string errorMessage) => new(errorCode, errorMessage, errorDetails: null);

    public static ErrorResponse New(string errorCode, string errorMessage, ValidationResult validationResult) =>
        new(
            errorCode,
            errorMessage,
            errorDetails: validationResult.Errors.Select(x => new ErrorResponse(x.PropertyName, x.ErrorMessage, errorDetails: null))
        );
}
