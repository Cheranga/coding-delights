namespace AzureServiceBusLib.Core;

/// <summary>
/// Represents a dual-type operation response that can hold either of two specific <see cref="OperationResult"/> types.
/// This class provides a type-safe way to handle operations that can result in two different but valid outcomes.
/// </summary>
/// <typeparam name="TA">The first type of <see cref="OperationResult"/> that this response can contain.</typeparam>
/// <typeparam name="TB">The second type of <see cref="OperationResult"/> that this response can contain.</typeparam>
public sealed class OperationResponse<TA, TB>
    where TA : OperationResult
    where TB : OperationResult
{
    /// <summary>
    /// Gets the underlying <see cref="OperationResult"/> instance, which can be either of type TA or TB.
    /// The result represents the outcome of the operation and can be either successful or failed.
    /// </summary>
    public OperationResult Result { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationResponse{TA, TB}"/> class with a specific <see cref="OperationResult"/>.
    /// </summary>
    /// <param name="result">The <see cref="OperationResult"/> instance to wrap in this response.</param>
    private OperationResponse(OperationResult result)
    {
        Result = result;
    }

    /// <summary>
    /// Implicitly converts a result of type TA to an <see cref="OperationResponse{TA, TB}"/>.
    /// Enables automatic wrapping of the first result type without explicit conversion.
    /// </summary>
    /// <param name="a">The first type of <see cref="OperationResult"/> to convert.</param>
    /// <returns>A new <see cref="OperationResponse{TA, TB}"/> containing the provided result.</returns>
    public static implicit operator OperationResponse<TA, TB>(TA a)
    {
        return new OperationResponse<TA, TB>(a);
    }

    /// <summary>
    /// Implicitly converts a result of type TB to an <see cref="OperationResponse{TA, TB}"/>.
    /// Enables automatic wrapping of the second result type without explicit conversion.
    /// </summary>
    /// <param name="b">The second type of <see cref="OperationResult"/> to convert.</param>
    /// <returns>A new <see cref="OperationResponse{TA, TB}"/> containing the provided result.</returns>
    public static implicit operator OperationResponse<TA, TB>(TB b)
    {
        return new OperationResponse<TA, TB>(b);
    }
}
