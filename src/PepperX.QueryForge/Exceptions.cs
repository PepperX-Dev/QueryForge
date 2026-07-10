namespace PepperX.QueryForge;

/// <summary>
/// The exception thrown when a <see cref="Query"/> fails validation rules 
/// and the validation mode is set to <see cref="QueryValidationMode.ThrowException"/>.
/// </summary>
public class QueryValidationException : Exception
{
    /// <summary>Gets the list of specific properties or column names that violated the validation rules.</summary>
    public IReadOnlyList<string> InvalidProperties { get; }

    /// <summary>Initializes a new instance of the <see cref="QueryValidationException"/> class.</summary>
    public QueryValidationException(string message, IReadOnlyList<string> invalidProperties)
        : base(message)
    {
        InvalidProperties = invalidProperties;
    }
}