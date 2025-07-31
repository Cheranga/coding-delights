using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using AutoBogus;
using OrderProcessorFuncApp.Core;
using OrderProcessorFuncApp.Domain;
using OrderProcessorFuncApp.Domain.Models;
using OrderProcessorFuncApp.Features.CreateOrder;
using OrderProcessorFuncApp.Features.ProcessOrder;

namespace OrderProcessorFuncApp.Integration.Tests;

[Collection(FunctionsTestFixtureCollection.Name)]
public class OrderProcessorIntegrationTests(IsolatedFunctionsTestFixture fixture)
{
    [Fact(DisplayName = "Valid order creation should return Accepted status")]
    public async Task CreateOrder_ShouldReturnAccepted()
    {
        var createOrderRequestDto = new AutoFaker<CreateOrderRequestDto>().Generate();
        var serialized = JsonSerializer.Serialize(createOrderRequestDto);
        var jsonContent = new StringContent(serialized, Encoding.UTF8, MediaTypeNames.Application.Json);
        var response = await fixture.Client.PostAsync("/api/orders", jsonContent);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        await Task.Delay(TimeSpan.FromSeconds(5));
        var functionLogs = await fixture.GetFunctionLogs();
        Assert.Contains(
            $"{nameof(AsbProcessOrderFunction)} processing message body:",
            functionLogs.StdOut,
            StringComparison.OrdinalIgnoreCase
        );
    }

    [Fact(DisplayName = "Invalid order creation should return BadRequest status")]
    public async Task CreateOrder_ShouldReturnBadRequest_WhenInvalidData()
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

        var serialized = JsonSerializer.Serialize(invalidDto);
        var jsonContent = new StringContent(serialized, Encoding.UTF8, MediaTypeNames.Application.Json);
        var response = await fixture.Client.PostAsync("/api/orders", jsonContent);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(errorResponse);
        Assert.Equal(ErrorCodes.InvalidDataInRequest, errorResponse.ErrorCode);
        Assert.NotNull(errorResponse.ErrorDetails);
        Assert.NotEmpty(errorResponse.ErrorDetails);
    }
}
