namespace PopfileNet.Backend.Models;

/// <summary>
/// Represents error information in an API response.
/// </summary>
public class ApiError
{
    /// <summary>
    /// Gets the error code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiError"/> class.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    public ApiError(string code, string message)
    {
        Code = code;
        Message = message;
    }
}
