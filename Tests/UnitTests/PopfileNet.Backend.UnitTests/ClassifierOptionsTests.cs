using PopfileNet.Backend.Services;
using Shouldly;
using Xunit;

namespace PopfileNet.Backend.UnitTests;

public class ClassifierOptionsTests
{
    [Fact]
    public void Defaults_AreApplied()
    {
        var options = new ClassifierOptions();

        options.ModelsRoot.ShouldBe("classifier-models");
        options.MaxCachedModels.ShouldBe(16);
        options.CacheTtl.ShouldBe(TimeSpan.FromMinutes(20));
    }

    [Fact]
    public void MaxCachedModels_Zero_IsClampedToOne()
    {
        var options = new ClassifierOptions { MaxCachedModels = 0 };
        options.MaxCachedModels.ShouldBe(1);
    }

    [Fact]
    public void MaxCachedModels_Negative_IsClampedToOne()
    {
        var options = new ClassifierOptions { MaxCachedModels = -5 };
        options.MaxCachedModels.ShouldBe(1);
    }

    [Fact]
    public void CacheTtl_Negative_IsClampedToZero()
    {
        var options = new ClassifierOptions { CacheTtl = TimeSpan.FromSeconds(-30) };
        options.CacheTtl.ShouldBe(TimeSpan.Zero);
    }
}