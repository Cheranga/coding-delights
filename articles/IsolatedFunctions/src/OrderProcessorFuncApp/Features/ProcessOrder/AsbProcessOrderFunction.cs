using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace OrderProcessorFuncApp.Features.ProcessOrder;

public class AsbProcessOrderFunction
{
    private readonly ILogger<AsbProcessOrderFunction> _logger;

    public AsbProcessOrderFunction(ILogger<AsbProcessOrderFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(AsbProcessOrderFunction))]
    public async Task Run(
        [ServiceBusTrigger("%ServiceBusConfig:ProcessingQueueName%", Connection = "AsbConnection", AutoCompleteMessages = false)]
            ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions
    )
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
        _logger.LogInformation("Message ID: {Id}", message.MessageId);
        _logger.LogInformation("{FunctionName} processing message body: {Body}", nameof(AsbProcessOrderFunction), message.Body);
        _logger.LogInformation("Message Content-Type: {ContentType}", message.ContentType);

        // Complete the message
        await messageActions.CompleteMessageAsync(message);
    }
}
