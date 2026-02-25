using System;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Data;
using PopfileNet.Common;

namespace PopfileNet.Classifier;

public class NaiveBayesianClassifier {
    private readonly MLContext _mlContext = new();
    private ITransformer? _modelForPrediction;
    private ITransformer? _featureTransformer;
    private ITransformer? _trainerTransformer;
    private ITransformer? _postTransformer;

    public void Train(EmailClassificationDataSet trainingData)
    {
        if (trainingData == null) throw new ArgumentNullException(nameof(trainingData));

        var dataEnumerable = trainingData.Data ?? Enumerable.Empty<EmailTrainingData>();
        if (!dataEnumerable.Any())
            throw new InvalidOperationException("Training data is empty. Add training samples before calling Train().");

        var dataView = _mlContext.Data.LoadFromEnumerable(dataEnumerable);

        // Step 1: Feature transformation pipeline
        var featureEstimator = _mlContext.Transforms.Concatenate("TextForFeaturize", "Subject", "Content")
            .Append(_mlContext.Transforms.Text.FeaturizeText("Features", "TextForFeaturize"));

        _featureTransformer = featureEstimator.Fit(dataView);
        var transformedData = _featureTransformer.Transform(dataView);

        // Step 2: Label mapping (only during training)
        var labelMapperEstimator = _mlContext.Transforms.Conversion.MapValueToKey("Label");
        var labelMapperTransformer = labelMapperEstimator.Fit(transformedData);
        var mappedData = labelMapperTransformer.Transform(transformedData);

        // Step 3: Train the model
        var trainer = _mlContext.MulticlassClassification.Trainers.NaiveBayes(labelColumnName: "Label", featureColumnName: "Features");
        _trainerTransformer = trainer.Fit(mappedData);

        // Step 4: Post-processing (map keys back to original values)
        var postEstimator = _mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel");
        _postTransformer = postEstimator.Fit(_trainerTransformer.Transform(mappedData));

        // Prediction model: features -> trainer -> post (without label mapping, so no Label column needed)
        _modelForPrediction = _featureTransformer.Append(_trainerTransformer).Append(_postTransformer);
    }

    public EmailPrediction Predict(Email emailContent)
    {
        if (_modelForPrediction == null)
            throw new InvalidOperationException("Model not trained. Call Train() before Predict().");

        // Create a single-row IDataView from the EmailInput (note: no Label field required)
        var input = Mapping.MapToInput(emailContent);
        var inputDv = _mlContext.Data.LoadFromEnumerable(new[] { input });

        // Apply clean prediction pipeline: features -> trainer -> post-transform (no label mapping on input)
        var outputDv = _modelForPrediction.Transform(inputDv);

        // Extract prediction manually from output columns
        using var cursor = outputDv.GetRowCursor(outputDv.Schema);
        var labelColumn = outputDv.Schema.GetColumnOrNull("PredictedLabel") ?? throw new InvalidOperationException("PredictedLabel column not found");
        // Try "Score" first, then "Scores" (both column names are used by different trainers)
        var scoreColumn = outputDv.Schema.GetColumnOrNull("Score") ?? outputDv.Schema.GetColumnOrNull("Scores") ?? throw new InvalidOperationException("Score/Scores column not found");
        
        var labelGetter = cursor.GetGetter<ReadOnlyMemory<char>>(labelColumn);
        var scoreGetter = cursor.GetGetter<VBuffer<float>>(scoreColumn);

        if (!cursor.MoveNext())
            throw new InvalidOperationException("No predictions generated");

        ReadOnlyMemory<char> predictedLabelMemory = default;
        VBuffer<float> scores = default;
        labelGetter(ref predictedLabelMemory);
        scoreGetter(ref scores);

        return new EmailPrediction
        {
            PredictedLabel = predictedLabelMemory.ToString(),
            Scores = scores.DenseValues().ToArray()
        };
    }
}