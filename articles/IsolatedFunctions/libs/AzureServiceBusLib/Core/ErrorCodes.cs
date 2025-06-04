namespace AzureServiceBusLib.Core;

internal static class ErrorCodes
{
    public const string MessagePublishError = nameof(MessagePublishError);
    public const string TooManyMessagesInBatch = nameof(TooManyMessagesInBatch);
}
