# Azure Functions

## Todo

* Adding tests for OrderPublisher.Console
  * Use `TestContainers.Net` to run a local instance of the Azure Service Bus
  * Create the session-based topic and subscription
  * Publish a message to the topic
  * Verify that the message is received by the subscription

## Notes

### Using TestContainers.ServiceBus

* You cannot use the `ServiceBusAdministrationClient` in here
  * Behind the scenes, it uses a mocked Azure Service Bus Emulator, and it only supports data plane operations such as sending and receiving messages
  * It does not support management operations such as creating queues, topics, or subscriptions
* You must provide a configuration file when configuring the container according to the documentation