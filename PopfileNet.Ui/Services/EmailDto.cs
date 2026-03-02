namespace PopfileNet.Ui.Services;

public record EmailDto(string Id, string Subject, string FromAddress, DateTime ReceivedDate, string BucketName);
