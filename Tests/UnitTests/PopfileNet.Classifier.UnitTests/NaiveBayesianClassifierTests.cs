using FluentAssertions;
using PopfileNet.Common;
using Xunit;

namespace PopfileNet.Classifier.Tests;

public class NaiveBayesianClassifierTests
{
    [Fact]
    public void Train_ValidData_Success()
    {
        var classifier = new NaiveBayesianClassifier();
        var trainingData = new EmailClassificationDataSet();
        
        trainingData.AddMail(CreateSampleEmail("Newsletter about products", "Buy our new products now!"), "spam");
        trainingData.AddMail(CreateSampleEmail("Meeting tomorrow", "Let's schedule a meeting for tomorrow"), "ham");
        trainingData.AddMail(CreateSampleEmail("Special offer", "Get 50% off on all items"), "spam");

        classifier.Train(trainingData);

        classifier.Should().NotBeNull();
    }

    [Fact]
    public void Train_NullData_ThrowsArgumentNullException()
    {
        var classifier = new NaiveBayesianClassifier();

        var action = () => classifier.Train(null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("trainingData");
    }

    [Fact]
    public void Train_EmptyData_ThrowsInvalidOperationException()
    {
        var classifier = new NaiveBayesianClassifier();
        var trainingData = new EmailClassificationDataSet();

        var action = () => classifier.Train(trainingData);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Training data is empty*");
    }

    [Fact]
    public void Train_SingleLabel_Success()
    {
        var classifier = new NaiveBayesianClassifier();
        var trainingData = new EmailClassificationDataSet();
        
        trainingData.AddMail(CreateSampleEmail("Newsletter 1", "Content 1"), "spam");
        trainingData.AddMail(CreateSampleEmail("Newsletter 2", "Content 2"), "spam");

        classifier.Train(trainingData);

        classifier.Should().NotBeNull();
    }

    [Fact]
    public void Train_MultipleLabels_Success()
    {
        var classifier = new NaiveBayesianClassifier();
        var trainingData = new EmailClassificationDataSet();
        
        trainingData.AddMail(CreateSampleEmail("Newsletter", "Buy now"), "spam");
        trainingData.AddMail(CreateSampleEmail("Meeting", "Let's meet"), "work");
        trainingData.AddMail(CreateSampleEmail("Family update", "How are you"), "personal");
        trainingData.AddMail(CreateSampleEmail("Special deal", "Limited offer"), "spam");

        classifier.Train(trainingData);

        classifier.Should().NotBeNull();
    }

    [Fact]
    public void Predict_TrainedModel_ReturnsPrediction()
    {
        var classifier = new NaiveBayesianClassifier();
        var trainingData = new EmailClassificationDataSet();
        
        trainingData.AddMail(CreateSampleEmail("Newsletter about products", "Buy our new products now!"), "spam");
        trainingData.AddMail(CreateSampleEmail("Meeting tomorrow", "Let's schedule a meeting for tomorrow"), "ham");
        
        classifier.Train(trainingData);

        var email = CreateSampleEmail("New meeting", "Let's meet next week");
        var result = classifier.Predict(email);

        result.Should().NotBeNull();
        result.PredictedLabel.Should().NotBeNullOrEmpty();
        result.Scores.Should().NotBeEmpty();
    }

    [Fact]
    public void Predict_UntrainedModel_ThrowsInvalidOperationException()
    {
        var classifier = new NaiveBayesianClassifier();
        var email = CreateSampleEmail("Test", "Test content");

        var action = () => classifier.Predict(email);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Model not trained*");
    }

    [Fact]
    public void Predict_NullEmail_ThrowsArgumentNullException()
    {
        var classifier = new NaiveBayesianClassifier();
        var trainingData = new EmailClassificationDataSet();
        
        trainingData.AddMail(CreateSampleEmail("Test", "Test"), "spam");
        classifier.Train(trainingData);

        var action = () => classifier.Predict(null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Predict_ReturnsValidPrediction()
    {
        var classifier = new NaiveBayesianClassifier();
        var trainingData = new EmailClassificationDataSet();
        
        trainingData.AddMail(CreateSampleEmail("Newsletter about products", "Buy our new products now!"), "spam");
        trainingData.AddMail(CreateSampleEmail("Meeting tomorrow", "Let's schedule a meeting for tomorrow"), "ham");
        
        classifier.Train(trainingData);

        var email = CreateSampleEmail("New meeting", "Let's meet next week");
        var result = classifier.Predict(email);

        result.Scores.Should().NotBeEmpty();
        result.Scores.Length.Should().Be(2);
        result.PredictedLabel.Should().BeOneOf("spam", "ham");
    }

    [Fact]
    public void Predict_EmptyEmail_ReturnsPrediction()
    {
        var classifier = new NaiveBayesianClassifier();
        var trainingData = new EmailClassificationDataSet();
        
        trainingData.AddMail(CreateSampleEmail("Newsletter", "Content"), "spam");
        trainingData.AddMail(CreateSampleEmail("Meeting", "Content"), "ham");
        
        classifier.Train(trainingData);

        var email = new Email { Subject = "", Body = "" };
        var result = classifier.Predict(email);

        result.Should().NotBeNull();
        result.PredictedLabel.Should().NotBeNullOrEmpty();
    }

    private static Email CreateSampleEmail(string subject, string body)
    {
        return new Email
        {
            Id = Guid.NewGuid().ToString(),
            Subject = subject,
            Body = body,
            FromAddress = "test@example.com",
            ToAddresses = "recipient@example.com",
            ReceivedDate = DateTime.Now
        };
    }
}
