using PopfileNet.Common;

namespace PopfileNet.Classifier;

public class EmailClassificationDataSet
{
    private readonly List<EmailTrainingData> _trainingData = new();

    public IEnumerable<EmailTrainingData> Data => _trainingData;

    public void AddMail(Email mail, string label)
    {
        _trainingData.Add(new EmailTrainingData
        {
            Subject = mail.Subject,
            Content = mail.Body,
            Label = label
        });
    }
}