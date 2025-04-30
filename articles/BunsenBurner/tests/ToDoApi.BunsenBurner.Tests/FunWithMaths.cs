using static BunsenBurner.ArrangeActAssert;

namespace ToDoApi.BunsenBurner.Tests;

public static class FunWithMaths
{
    [Theory(DisplayName = "Adding numbers")]
    [InlineData(1, 1, 2)]
    [InlineData(1, -1, 0)]
    public static async Task TestAddWithBunsen(int a, int b, int expected) =>
        await Arrange(() => (a, b)).Act(input => input.a + input.b).Assert(result => result == expected);

    [Theory(DisplayName = "Adding numbers")]
    [InlineData(1, 1, 2)]
    [InlineData(1, -1, 0)]
    public static void TestAdd(int a, int b, int expected)
    {
        // Arrange (the input data is provided)

        // Act
        var result = a + b;

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact(DisplayName = "Division by zero")]
    public static async Task DivisionByZeroBunsen() =>
        // ReSharper disable once IntDivisionByZero
        await 1
            .Arrange()
            .Act(x => x / 0)
            .Throw<DivideByZeroException>()
            .Assert(exception => exception.Message == "Attempted to divide by zero.");

    [Fact(DisplayName = "Division by zero")]
    public static void DivisionByZero()
    {
        // Arrange
        var denominator = 0;

        // Act and Assert
        var exception = Assert.Throws<DivideByZeroException>(() => 1 / denominator);
        Assert.Equal("Attempted to divide by zero.", exception.Message);
    }
}
