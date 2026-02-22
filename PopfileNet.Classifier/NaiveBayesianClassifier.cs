using Microsoft.ML;

namespace PopfileNet.Classifier;

public partial class NaiveBayesianClassifier {
    private MLContext _mlContext = new MLContext();
    private ITransformer? _model;

    public void Train(string trainingDataPath) {
        var dataView = _mlContext.Data.LoadFromTextFile<EmailTrainingData>(
            trainingDataPath,
            hasHeader: true);

        // Simplified Naive Bayes trainer call
        var pipeline = _mlContext.MulticlassClassification.Trainers.NaiveBayes();
        _model = pipeline.Fit(dataView);
    }

    public EmailPrediction Predict(string emailContent) {
        if (_model == null)
            throw new InvalidOperationException("Model not trained");

        var predictionEngine = 
            _mlContext.Model.CreatePredictionEngine<EmailInput, EmailPrediction>(_model);

        return predictionEngine.Predict(new EmailInput { Content = emailContent });
    }
}