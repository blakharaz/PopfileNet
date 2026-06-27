namespace PopfileNet.Backend.Models;

public record MismatchDetailDto(
    string EmailId,
    string Subject,
    string ActualBucket,
    string PredictedBucket);
