namespace OrderProcessorFuncApp.Integration.Tests;

[CollectionDefinition(Name, DisableParallelization = false)]
public class FunctionsTestFixtureCollection : ICollectionFixture<IsolatedFunctionsTestFixture>
{
    public const string Name = nameof(FunctionsTestFixtureCollection);
}
