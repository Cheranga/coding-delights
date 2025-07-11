using System.Text.Json.Serialization;
using FluentValidation.Results;

namespace OrderProcessorFuncApp.Core.Shared;

public sealed record ErrorResponse
{
    [JsonConstructor]
    public ErrorResponse(string errorMessage, string errorCode, IReadOnlyCollection<ErrorResponse>? errorDetails)
    {
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
        ErrorDetails = errorDetails?.ToList();
    }

    public string ErrorMessage { get; init; }

    public string ErrorCode { get; init; }

    public IReadOnlyCollection<ErrorResponse>? ErrorDetails { get; init; }

    public static ErrorResponse New(string errorCode, string errorMessage) => new(errorMessage, errorCode, errorDetails: null);

    public static ErrorResponse New(string errorCode, string errorMessage, ValidationResult validationResult) =>
        new(
            errorMessage,
            errorCode,
            errorDetails: validationResult
                .Errors.Select(x => new ErrorResponse(
                    x.FormattedMessagePlaceholderValues.TryGetValue("PropertyName", out var propertyName)
                        ? propertyName?.ToString() ?? x.PropertyName
                        : x.PropertyName,
                    x.ErrorMessage,
                    errorDetails: null
                ))
                .ToList()
        );
}
