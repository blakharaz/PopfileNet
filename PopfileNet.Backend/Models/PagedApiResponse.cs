namespace PopfileNet.Backend.Models;

/// <summary>
/// Represents a paged API response with collection data and pagination metadata.
/// </summary>
/// <typeparam name="T">The type of items in the collection.</typeparam>
public class PagedApiResponse<T>
{
    /// <summary>
    /// Gets the collection of items for the current page.
    /// </summary>
    public IEnumerable<T> Items { get; private set; } = [];

    /// <summary>
    /// Gets the current page number (1-based).
    /// </summary>
    public int Page { get; private set; }

    /// <summary>
    /// Gets the number of items per page.
    /// </summary>
    public int PageSize { get; private set; }

    /// <summary>
    /// Gets the total number of items across all pages.
    /// </summary>
    public int TotalCount { get; private set; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPrevious => Page > 1;

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNext => Page < TotalPages;

    /// <summary>
    /// Gets the error information if the response failed.
    /// </summary>
    public ApiError? Error { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the response was successful.
    /// </summary>
    public bool IsSuccess => Error == null;

    private PagedApiResponse() { }

    /// <summary>
    /// Creates a successful paged response.
    /// </summary>
    /// <param name="items">The items for the current page.</param>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="totalCount">The total number of items.</param>
    /// <returns>A successful paged API response.</returns>
    public static PagedApiResponse<T> Success(IEnumerable<T> items, int page, int pageSize, int totalCount) => new()
    {
        Items = items,
        Page = page,
        PageSize = pageSize,
        TotalCount = totalCount
    };

    /// <summary>
    /// Creates a failed paged response.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <returns>A failed paged API response.</returns>
    public static PagedApiResponse<T> Failure(string code, string message) => new()
    {
        Error = new ApiError(code, message)
    };
}
