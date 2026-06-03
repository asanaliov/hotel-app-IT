using System.Reflection;
using HotelApp.Tests.Utils;

namespace HotelApp.Tests;

public class GlobalTestFixture : IAsyncLifetime
{
    private static readonly ExamLogger _logger = new(
        studentId: "YOUR_INDEX_HERE", // <---------- PUT YOUR INDEX HERE
        logFilePath: Path.Combine(
            Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.Parent?.FullName ?? "",
            "TestOutput",
            "test_results.json"));

    public ExamLogger Logger => _logger;

    private static readonly Dictionary<string, DateTime> _testStartTimes = new();

    public GlobalTestFixture()
    {
        var projectPath = Directory.GetParent(AppContext.BaseDirectory)
                              ?.Parent?.Parent?.Parent?.Parent?.FullName
                          ?? throw new DirectoryNotFoundException("Could not find project root.");

        Directory.CreateDirectory(Path.Combine(projectPath, "TestOutput"));
    }

    public virtual Task InitializeAsync() => Task.CompletedTask;

    public virtual async Task DisposeAsync()
    {
        await Logger.SaveToFileAsync();
        Logger.PrintSummary();
        Logger.FlushOutput();
    }

    public void BeginTest(string testName)
    {
        _testStartTimes[testName] = DateTime.Now;
    }

    public void EndTest(string testName, string category, int points, bool passed, string? errorMessage = null)
    {
        Logger.LogTestResult(testName, category, passed, errorMessage, points);
    }

    public (string category, int points) GetTestMetadata(string testName, object testClassInstance)
    {
        var type = testClassInstance.GetType();
        var method = type.GetMethod(testName);

        if (method == null)
            return ("Unknown", 1);

        var factAttr = method.GetCustomAttribute<LoggedFactAttribute>();

        if (factAttr != null)
            return (factAttr.Category, factAttr.Points);

        return ("Default", 1);
    }
}