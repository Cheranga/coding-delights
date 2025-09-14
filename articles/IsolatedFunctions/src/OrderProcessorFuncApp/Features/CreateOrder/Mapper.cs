using OrderProcessorFuncApp.Domain.Http;
using OrderProcessorFuncApp.Domain.Messaging;

namespace OrderProcessorFuncApp.Features.CreateOrder;

internal static class Mapper
{
    internal static ProcessOrderMessage ToMessage(this CreateOrderRequestDto request)
    {
        var message = new ProcessOrderMessage
        {
            ReferenceId = request.ReferenceId,
            Items = request.Items,
            OrderReceivedAt = request.OrderDate,
        };

        return message;
    }
}
