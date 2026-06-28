using PopfileNet.Backend.Models;
using Shouldly;
using Xunit;

namespace PopfileNet.Backend.UnitTests;

public class EvaluationRequestTests
{
    [Fact]
    public void EvaluationRequest_HasCorrectDefaults()
    {
        // Arrange & Act
        var request = new EvaluationRequest();

        // Assert
        request.FolderFilter.ShouldBe("all");
        request.BucketFilter.ShouldBe("all");
        request.CutoffType.ShouldBe("date");
        request.CutoffValue.ShouldBeNull();
        request.TrainTestSplit.ShouldBe(0.8f);
        request.NumberOfRuns.ShouldBe(1);
    }

    [Fact]
    public void EvaluationRequest_AcceptsCustomValues()
    {
        var request = new EvaluationRequest(
            FolderFilter: "Inbox",
            BucketFilter: "work-bucket",
            CutoffType: "amount",
            CutoffValue: "100",
            TrainTestSplit: 0.7f,
            NumberOfRuns: 5);

        // Assert
        request.FolderFilter.ShouldBe("Inbox");
        request.BucketFilter.ShouldBe("work-bucket");
        request.CutoffType.ShouldBe("amount");
        request.CutoffValue.ShouldBe("100");
        request.TrainTestSplit.ShouldBe(0.7f);
        request.NumberOfRuns.ShouldBe(5);
    }

    [Fact]
    public void EvaluationRequest_ImplementsValueEquality()
    {
        var a = new EvaluationRequest(CutoffType: "amount", CutoffValue: "10");
        var b = new EvaluationRequest(CutoffType: "amount", CutoffValue: "10");

        (a == b).ShouldBeTrue();
    }

    [Fact]
    public void EvaluationRequest_ImplementsHashCode()
    {
        // Records should have hash codes that respect value equality
        var a = new EvaluationRequest(CutoffType: "amount", CutoffValue: "10");
        var b = new EvaluationRequest(CutoffType: "amount", CutoffValue: "10");

        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void EvaluationRequest_CanBeUsedInDictionary()
    {
        // Records should work as dictionary keys based on value equality
        var a = new EvaluationRequest(CutoffType: "amount", CutoffValue: "10");
        var b = new EvaluationRequest(CutoffType: "amount", CutoffValue: "10");

        var dict = new Dictionary<EvaluationRequest, string> { [a] = "value" };

        dict[b].ShouldBe("value");
    }
}
