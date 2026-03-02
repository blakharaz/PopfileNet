namespace PopfileNet.Ui.Services;

public class ApiError(string Code, string Message)
{
    public string Code { get; } = Code;
    public string Message { get; } = Message;
}
