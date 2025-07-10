using System.Net;
using AutoBogus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OrderProcessorFuncApp.Core.Http;
using OrderProcessorFuncApp.Features.CreateOrder;

namespace OrderProcessorFuncApp.Tests;

public static class CreateOrderFunctionTests
{
    [Fact(DisplayName = "Valid create order request returns accepted response")]
    public static async Task Test1()
    {
        var createOrderRequest = new AutoFaker<CreateOrderRequestDto>().Generate();

        var host = Bootstrapper.GetHost(customRegistrations: (_, services) => services.AddSingleton<CreateOrderFunction>());
        var serviceProvider = host.Services;
        var context = new Mock<FunctionContext>();
        context.Setup(x => x.CancellationToken).Returns(CancellationToken.None);
        context.Setup(x => x.Items).Returns(new Dictionary<object, object> { { "Dto", createOrderRequest } });
        context.Setup(x => x.InstanceServices).Returns(serviceProvider);
        var request = new TestHttpRequestData<CreateOrderRequestDto>(context.Object, createOrderRequest);

        var function = serviceProvider.GetRequiredService<CreateOrderFunction>();
        var response = await function.Run(request, context.Object);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }
}
