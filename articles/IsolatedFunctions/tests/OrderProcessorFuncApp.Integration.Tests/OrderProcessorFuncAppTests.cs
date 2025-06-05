using System.Net;
using AutoBogus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using OrderProcessorFuncApp.Features.CreateOrder;

namespace OrderProcessorFuncApp.Integration.Tests;

public class OrderProcessorFuncAppTests
{
    private readonly IHost _host;

    public OrderProcessorFuncAppTests()
    {
        _host = Bootstrapper.GetHost(customRegistrations: (_, services) => services.AddSingleton<CreateOrderFunction>());
    }

    [Fact]
    public async Task Test1() =>
        await Arrange(() =>
            {
                var mockedContext = new Mock<FunctionContext>();
                mockedContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

                var createOrderRequestDto = new AutoFaker<CreateOrderRequestDto>().Generate();
                var httpRequestData = new TestHttpRequestData<CreateOrderRequestDto>(mockedContext.Object, createOrderRequestDto);

                var serviceProvider = _host.Services;
                var createOrderFunction = serviceProvider.GetRequiredService<CreateOrderFunction>();

                return (context: mockedContext.Object, httpRequestData, createOrderFunction);
            })
            .Act(async data =>
            {
                var response = await data.createOrderFunction.Run(data.httpRequestData, data.context);
                return response;
            })
            .Assert(result => result.HttpResponse != null && result.HttpResponse.StatusCode == HttpStatusCode.Accepted);
}
