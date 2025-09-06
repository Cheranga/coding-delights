namespace OrderProcessorFuncApp.Domain;

internal static class ErrorMessages
{
    public const string InvalidRequestSchema = "The request does not conform to the schema";
    public const string InvalidDataInRequest = "Invalid data in request";
    public const string ErrorOccurredWhenProcessingOrder = "An error occurred when processing the order";
    public const string CannotDeserialize = "Cannot deserialize the request body";
    public const string Unknown = "Unknown";
    public const string ErrorOccurredWhenPublishingToQueue = "An error occurred when publishing to the queue";
}
