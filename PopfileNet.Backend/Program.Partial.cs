// needs to be partial to allow tests to toggle behaviour
public static partial class Program
{
    /// <summary>
    /// When true the database initialization block in <c>Program</c> is skipped.
    /// Tests set this flag before constructing a <see cref="WebApplicationFactory{Program}"/>.
    /// </summary>
    public static bool SkipDbInitForTests { get; set; }
}
