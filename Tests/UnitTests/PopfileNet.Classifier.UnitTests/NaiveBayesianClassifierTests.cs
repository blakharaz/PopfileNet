using Shouldly;
using PopfileNet.Common;
using Xunit;

namespace PopfileNet.Classifier.UnitTests;

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

        classifier.ShouldNotBeNull();
    }

    [Fact]
    public void Train_NullData_ThrowsArgumentNullException()
    {
        var classifier = new NaiveBayesianClassifier();

        var action = () => classifier.Train(null!);

        action.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void Train_EmptyData_ThrowsInvalidOperationException()
    {
        var classifier = new NaiveBayesianClassifier();
        var trainingData = new EmailClassificationDataSet();

        var action = () => classifier.Train(trainingData);

        action.ShouldThrow<InvalidOperationException>()
            .Message.ShouldContain("Training data is empty");
    }

    [Fact]
    public void Train_SingleLabel_Success()
    {
        var classifier = new NaiveBayesianClassifier();
        var trainingData = new EmailClassificationDataSet();
        
        trainingData.AddMail(CreateSampleEmail("Newsletter 1", "Content 1"), "spam");
        trainingData.AddMail(CreateSampleEmail("Newsletter 2", "Content 2"), "spam");

        classifier.Train(trainingData);

        classifier.ShouldNotBeNull();
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

        classifier.ShouldNotBeNull();
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

        result.ShouldNotBeNull();
        result.PredictedLabel.ShouldNotBeNullOrEmpty();
        result.Scores.ShouldNotBeEmpty();
    }

    [Fact]
    public void Predict_UntrainedModel_ThrowsInvalidOperationException()
    {
        var classifier = new NaiveBayesianClassifier();
        var email = CreateSampleEmail("Test", "Test content");

        var action = () => classifier.Predict(email);

        action.ShouldThrow<InvalidOperationException>()
            .Message.ShouldContain("Model not trained");
    }

    [Fact]
    public void Predict_NullEmail_ThrowsArgumentNullException()
    {
        var classifier = new NaiveBayesianClassifier();
        var trainingData = new EmailClassificationDataSet();
        
        trainingData.AddMail(CreateSampleEmail("Test", "Test"), "spam");
        classifier.Train(trainingData);

        var action = () => classifier.Predict(null!);

        action.ShouldThrow<ArgumentNullException>();
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

        result.Scores.ShouldNotBeEmpty();
        result.Scores.Length.ShouldBe(2);
        result.PredictedLabel.ShouldBeOneOf("spam", "ham");
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

        result.ShouldNotBeNull();
        result.PredictedLabel.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void IsTrained_FalseBeforeTrain_ReturnsFalse()
    {
        var classifier = new NaiveBayesianClassifier();

        classifier.IsTrained.ShouldBeFalse();
    }

    [Fact]
    public void IsTrained_AfterTrain_ReturnsTrue()
    {
        var classifier = CreateTrainedClassifier();

        classifier.IsTrained.ShouldBeTrue();
    }

    [Fact]
    public void TrainingSampleCount_AfterTrain_ReturnsSampleCount()
    {
        var classifier = new NaiveBayesianClassifier();
        var trainingData = new EmailClassificationDataSet();
        trainingData.AddMail(CreateSampleEmail("Newsletter", "Content"), "spam");
        trainingData.AddMail(CreateSampleEmail("Meeting", "Content"), "ham");

        classifier.Train(trainingData);

        classifier.TrainingSampleCount.ShouldBe(2);
    }

    [Fact]
    public void Save_TrainedModel_WritesNonEmptyStream()
    {
        var classifier = CreateTrainedClassifier();
        using var stream = new MemoryStream();

        classifier.Save(stream);

        stream.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Save_UntrainedModel_ThrowsInvalidOperationException()
    {
        var classifier = new NaiveBayesianClassifier();
        using var stream = new MemoryStream();

        var action = () => classifier.Save(stream);

        action.ShouldThrow<InvalidOperationException>()
            .Message.ShouldContain("Model not trained");
    }

    [Fact]
    public void Save_NullStream_ThrowsArgumentNullException()
    {
        var classifier = CreateTrainedClassifier();

        var action = () => classifier.Save(null!);

        action.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void Load_NullStream_ThrowsArgumentNullException()
    {
        var classifier = new NaiveBayesianClassifier();

        var action = () => classifier.Load(null!);

        action.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void Load_ValidModel_ThenPredict_ReturnsPrediction()
    {
        var original = CreateTrainedClassifier();
        using var stream = new MemoryStream();
        original.Save(stream);

        var loaded = new NaiveBayesianClassifier();
        stream.Position = 0;
        loaded.Load(stream);

        loaded.IsTrained.ShouldBeTrue();
        var result = loaded.Predict(CreateSampleEmail("New meeting", "Let's schedule a meeting for tomorrow"));
        result.ShouldNotBeNull();
        result.PredictedLabel.ShouldNotBeNullOrEmpty();
        result.Scores.ShouldNotBeEmpty();
    }

    [Fact]
    public void Predict_BeforeLoad_ThrowsInvalidOperationException()
    {
        var loaded = new NaiveBayesianClassifier();

        var action = () => loaded.Predict(CreateSampleEmail("Test", "Test content"));

        action.ShouldThrow<InvalidOperationException>()
            .Message.ShouldContain("Model not trained");
    }

    [Fact]
    public void Load_CorruptStream_ThrowsInvalidOperationException()
    {
        var classifier = new NaiveBayesianClassifier();
        using var stream = new MemoryStream([1, 2, 3, 4, 5, 6, 7, 8]);

        var action = () => classifier.Load(stream);

        action.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void TrainAfterLoad_ReplacesModel()
    {
        var original = CreateTrainedClassifier();
        using var stream = new MemoryStream();
        original.Save(stream);

        var reloaded = new NaiveBayesianClassifier();
        stream.Position = 0;
        reloaded.Load(stream);

        var trainingData = new EmailClassificationDataSet();
        trainingData.AddMail(CreateSampleEmail("New A", "New content A"), "alpha");
        trainingData.AddMail(CreateSampleEmail("New B", "New content B"), "beta");
        reloaded.Train(trainingData);

        reloaded.IsTrained.ShouldBeTrue();
        reloaded.TrainingSampleCount.ShouldBe(2);
        var result = reloaded.Predict(CreateSampleEmail("New A", "New content A"));
        result.PredictedLabel.ShouldBeOneOf("alpha", "beta");
    }

    private static NaiveBayesianClassifier CreateTrainedClassifier()
    {
        var classifier = new NaiveBayesianClassifier();
        var trainingData = new EmailClassificationDataSet();
        trainingData.AddMail(CreateSampleEmail("Newsletter about products", "Buy our new products now!"), "spam");
        trainingData.AddMail(CreateSampleEmail("Meeting tomorrow", "Let's schedule a meeting for tomorrow"), "ham");
        classifier.Train(trainingData);
        return classifier;
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
