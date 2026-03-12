namespace PopfileNet.FunctionalTests;

public static class TestServices
{
    private static readonly Lazy<AppServices> _instance = new(() => new AppServices());
    public static AppServices Instance => _instance.Value;
}