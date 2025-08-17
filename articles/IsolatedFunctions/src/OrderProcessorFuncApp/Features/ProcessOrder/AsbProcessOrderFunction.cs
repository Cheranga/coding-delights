using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OrderProcessorFuncApp.Features.CreateOrder;

namespace OrderProcessorFuncApp.Features.ProcessOrder;

public class AsbProcessOrderFunction
{
    private readonly ILogger<AsbProcessOrderFunction> _logger;

    private readonly JsonSerializerOptions _serializerOptions;

    public AsbProcessOrderFunction(JsonSerializerOptions serializerOptions, ILogger<AsbProcessOrderFunction> logger)
    {
        _serializerOptions = serializerOptions;
        _logger = logger;
    }

    [Function(nameof(AsbProcessOrderFunction))]
    public async Task Run(
        [ServiceBusTrigger("%ServiceBusConfig:ProcessingQueueName%", Connection = "AsbConnection", AutoCompleteMessages = false)]
            ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions
    )
    {
        var dto = message.Body.ToObjectFromJson<CreateOrderRequestDto>(_serializerOptions);
        await Task.Delay(TimeSpan.FromSeconds(1));
        _logger.LogInformation("Order Id: {Id}", dto!.OrderId);
        _logger.LogInformation("{FunctionName} processing message body: {Body}", nameof(AsbProcessOrderFunction), message.Body);
        _logger.LogInformation("Message Content-Type: {ContentType}", message.ContentType);

        // Complete the message
        await messageActions.CompleteMessageAsync(message);
    }
}
