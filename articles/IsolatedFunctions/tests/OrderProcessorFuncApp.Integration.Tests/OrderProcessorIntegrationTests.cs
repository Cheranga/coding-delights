using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using AutoBogus;
using OrderProcessorFuncApp.Core;
using OrderProcessorFuncApp.Domain;
using OrderProcessorFuncApp.Domain.Http;
using OrderProcessorFuncApp.Domain.Models;
using OrderProcessorFuncApp.Features.ProcessOrder;

namespace OrderProcessorFuncApp.Integration.Tests;

[Collection(FunctionsTestFixtureCollection.Name)]
public class OrderProcessorIntegrationTests(IsolatedFunctionsTestFixture fixture)
{
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    [Fact(DisplayName = "Valid order creation should return Accepted status")]
    public async Task CreateOrder_ShouldReturnAccepted()
    {
        await Arrange(() =>
            {
                var createOrderRequestDto = new AutoFaker<CreateOrderRequestDto>().Generate();
                return createOrderRequestDto;
            })
            .Act(async data =>
            {
                var serialized = JsonSerializer.Serialize(data, _serializerOptions);
                var jsonContent = new StringContent(serialized, Encoding.UTF8, MediaTypeNames.Application.Json);
                var response = await fixture.Client.PostAsync("/api/orders", jsonContent);

                // Publish the service bus message
                await fixture.PublishServiceBusMessage("temp-orders", data, _serializerOptions, CancellationToken.None);

                // Delay to allow the functions to process the message
                await Task.Delay(TimeSpan.FromSeconds(5));
                return response;
            })
            .Assert((_, result) => result.StatusCode == HttpStatusCode.Accepted)
            .And(
                async (data, _) =>
                {
                    var functionLogs = await fixture.GetFunctionLogs();

                    Assert.Contains($"Order Id: {data.OrderId}", functionLogs.StdOut, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains("Processing order message:", functionLogs.StdOut, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains(
                        $"{nameof(AsbProcessOrderFunction)} processing message body:",
                        functionLogs.StdOut,
                        StringComparison.OrdinalIgnoreCase
                    );
                }
            );
    }

    [Fact(DisplayName = "Invalid order creation should return BadRequest status")]
    public async Task CreateOrder_ShouldReturnBadRequest_WhenInvalidData()
    {
        await Arrange(() =>
            {
                var invalidDto = new CreateOrderRequestDto
                {
                    OrderId = Guid.Empty, // Invalid customer ID
                    Items = new List<OrderItem>
                    {
                        new()
                        {
                            ProductId = string.Empty,
                            Quantity = 0,
                            Price = 0,
                            Metric = string.Empty,
                        },
                    },
                };

                return invalidDto;
            })
            .Act(async data =>
            {
                var serialized = JsonSerializer.Serialize(data, _serializerOptions);
                var jsonContent = new StringContent(serialized, Encoding.UTF8, MediaTypeNames.Application.Json);
                var response = await fixture.Client.PostAsync("/api/orders", jsonContent);
                return response;
            })
            .Assert(result => result.StatusCode == HttpStatusCode.BadRequest)
            .And(async result =>
            {
                var errorResponse = await result.Content.ReadFromJsonAsync<ErrorResponse>();
                Assert.NotNull(errorResponse);
                Assert.Equal(ErrorCodes.InvalidDataInRequest, errorResponse.ErrorCode);
                Assert.NotNull(errorResponse.ErrorDetails);
                Assert.NotEmpty(errorResponse.ErrorDetails);
            });
    }
}
