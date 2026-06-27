namespace PopfileNet.Ui.Services;

public record MismatchDetailDto(
    string EmailId,
    string Subject,
    string ActualBucket,
    string PredictedBucket);
