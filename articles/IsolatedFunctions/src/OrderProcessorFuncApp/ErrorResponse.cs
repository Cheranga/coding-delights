namespace OrderProcessorFuncApp;

public sealed record ErrorResponse
{
    private ErrorResponse(string errorCode, string errorMessage)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public string ErrorMessage { get; init; }

    public string ErrorCode { get; init; }

    public static ErrorResponse New(string errorCode, string errorMessage) => new(errorCode, errorMessage);
}
