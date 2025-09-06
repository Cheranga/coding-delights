using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using OrderProcessorFuncApp.Domain.Messaging;

namespace OrderProcessorFuncApp.Features.CreateOrder;

public sealed record OrderAcceptedResponse
{
    [HttpResult]
    public required HttpResponseData HttpResponse { get; set; }

    [QueueOutput("%StorageConfig:ProcessingQueueName%", Connection = "AzureWebJobsQueueConnection")]
    public ProcessOrderMessage? Message { get; set; }
}
