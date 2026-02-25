using PopfileNet.Common;

namespace PopfileNet.Classifier;

public static class Mapping
{
    public static EmailTrainingData MapToTrainingData(Email email, string label)
    {
        return new EmailTrainingData
        {
            Content = email.Body,
            Subject = email.Subject,
            Label =  label
        };
    }

    public static EmailInput MapToInput(Email email)
    {
        return new EmailInput
        {
            Content = email.Body,
            Subject = email.Subject
        };
    }
}