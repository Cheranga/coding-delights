namespace OrderPublisher.Console;

internal record struct Unit
{
    public static Unit Instance { get; } = new Unit();
};
