namespace OrderProcessorFuncApp.Core.Shared;

internal static class ErrorCodes
{
    public const string InvalidRequestSchema = nameof(InvalidRequestSchema);
    public const string InvalidDataInRequest = nameof(InvalidDataInRequest);
    public const string ErrorOccurredWhenProcessingOrder = nameof(ErrorOccurredWhenProcessingOrder);
    public const string Unknown = nameof(Unknown);
}
