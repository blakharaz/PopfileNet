namespace PopfileNet.Ui.Services;

public record ClassifierStatus(bool IsTrained, int TrainingDataCount, string Source = "NotTrained", DateTime? TrainedAtUtc = null)
{
    public static ClassifierStatus Empty => new(false, 0);
}
