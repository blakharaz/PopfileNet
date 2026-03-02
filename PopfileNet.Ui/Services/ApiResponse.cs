namespace PopfileNet.Ui.Services;

public class ApiResponse<T>
{
    public T? Value { get; set; }
    public ApiError? Error { get; set; }
    public bool IsSuccess => Error == null;
}
