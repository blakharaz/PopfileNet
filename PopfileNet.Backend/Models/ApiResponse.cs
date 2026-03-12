namespace PopfileNet.Backend.Models;

/// <summary>
/// Represents a generic API response that can contain either a value or an error.
/// </summary>
/// <typeparam name="T">The type of the value being returned.</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Gets the value of the response if successful.
    /// </summary>
    public T? Value { get; set; }

    /// <summary>
    /// Gets the error information if the response failed.
    /// </summary>
    public ApiError? Error { get; set; }

    /// <summary>
    /// Gets a value indicating whether the response was successful.
    /// </summary>
    public bool IsSuccess => Error == null;

    public ApiResponse() { }

    /// <summary>
    /// Creates a successful response with the specified value.
    /// </summary>
    /// <param name="value">The value to return.</param>
    /// <returns>A successful API response.</returns>
    public static ApiResponse<T> Success(T value) => new() { Value = value };

    /// <summary>
    /// Creates a failed response with the specified error.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <returns>A failed API response.</returns>
    public static ApiResponse<T> Failure(string code, string message) => new() { Error = new ApiError(code, message) };
}
