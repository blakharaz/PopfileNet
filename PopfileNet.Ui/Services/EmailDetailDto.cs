namespace PopfileNet.Ui.Services;

public record EmailDetailDto(string Id, string Subject, string FromAddress, string ToAddresses, DateTime ReceivedDate, string Body, string BucketName);
