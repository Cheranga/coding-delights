using AzureServiceBusLib.Publish;
using AzureServiceBusLib.Tests.Models;
using Moq;

namespace AzureServiceBusLib.Tests.DI;

public sealed class MessagePublisherFactoryTests
{
    [Fact(DisplayName = "Get Message Publisher for a given message type using a name")]
    public async Task Test1()
    {
        await Arrange(() =>
            {
                var publishers = Enumerable
                    .Range(1, 10)
                    .Select(x =>
                    {
                        var publisher = new Mock<IMessagePublisher<CreateOrderMessage>>();
                        publisher.Setup(y => y.Name).Returns($"publisher{x}");
                        return publisher.Object;
                    })
                    .ToList();

                return publishers;
            })
            .And(data =>
            {
                var factory = new MessagePublisherFactory(data);
                return factory;
            })
            .Act(data =>
            {
                var actualPublishers = Enumerable.Range(1, 10).Select(x => data.GetPublisher<CreateOrderMessage>($"publisher{x}")).ToList();
                return actualPublishers;
            })
            .Assert(result => result != null)
            .And(result =>
            {
                var actualPublisherNames = result.Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
                Assert.True(actualPublisherNames.SetEquals(Enumerable.Range(1, 10).Select(x => $"publisher{x}")));
            });
    }

    [Fact(DisplayName = "Get Message Publisher for a given message type using its type name only")]
    public async Task Test2()
    {
        await Arrange(() =>
            {
                var publisher1 = Mock.Of<IMessagePublisher<CreateOrderMessage>>(publisher => publisher.Name == nameof(CreateOrderMessage));
                var publisher2 = Mock.Of<IMessagePublisher<CreateOrderMessage>>(publisher => publisher.Name == "blah");
                return new List<IMessagePublisher<CreateOrderMessage>> { publisher1, publisher2 };
            })
            .And(data =>
            {
                var factory = new MessagePublisherFactory(data);
                return factory;
            })
            .Act(data => data.GetPublisher<CreateOrderMessage>())
            .Assert(result => result != null);
    }
}
