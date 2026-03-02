namespace PopfileNet.Ui.Services;

public record ClassifierStatus(bool IsTrained, int TrainingDataCount)
{
    public static ClassifierStatus Empty => new(false, 0);
}
