using System.Text;
using System.Text.Json;

namespace HotelApp.Tests.Utils;

// Local-only grading logger. The original exam harness also zipped the solution
// and uploaded it to a FINKI server; that behaviour has been removed — results
// are only written to a local JSON file and printed to the test output.
public class ExamLogger
{
    private readonly List<TestResult> _testResults = new();
    private readonly StringBuilder _output = new();
    private readonly string _logFilePath;
    private readonly string _studentId;

    public ExamLogger(string studentId, string? logFilePath = null)
    {
        _studentId = studentId;
        _logFilePath = logFilePath ?? $"test_results_{studentId}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
    }

    public void LogTestResult(string testName, string category, bool isPassed,
        string? errorMessage = null, int pointsWorth = 1)
    {
        var result = new TestResult
        {
            TestName = testName,
            StudentId = _studentId,
            TestCategory = category,
            IsPassed = isPassed,
            ExecutedAt = DateTime.Now,
            ErrorMessage = errorMessage,
            Points = pointsWorth,
        };

        _testResults.Add(result);
        AppendTestResultToOutput(result);
    }

    // ── ANSI styling helpers ─────────────────────────────────
    private const string Reset = "[0m";
    private const string Bold = "[1m";
    private const string Dim = "[2m";
    private const string Red = "[31m";
    private const string Green = "[32m";
    private const string Yellow = "[33m";
    private const string Cyan = "[36m";
    private const string Grey = "[90m";

    private static string Color(string code, string text) => $"{code}{text}{Reset}";

    private void AppendTestResultToOutput(TestResult result)
    {
        var icon = result.IsPassed ? Color(Green, "✔") : Color(Red, "✗");
        var status = result.IsPassed ? Color(Green, "PASS") : Color(Red, "FAIL");
        var time = Color(Grey, $"[{result.ExecutedAt:HH:mm:ss}]");
        var cat = Color(Cyan, $"{result.TestCategory,-13}");
        var pts = Color(Grey, $"{result.Points,2} pts");

        _output.AppendLine($"  {icon} {status}  {time} {cat} {result.TestName} {pts}");
    }

    private static string Bar(double pct, int width = 24)
    {
        var filled = (int)Math.Round(pct / 100 * width);
        filled = Math.Clamp(filled, 0, width);
        var color = pct >= 80 ? Green : pct >= 50 ? Yellow : Red;
        return Color(color, new string('█', filled)) + Color(Grey, new string('░', width - filled));
    }

    public void PrintSummary()
    {
        var totalTests = _testResults.Count;
        var passedTests = _testResults.Count(r => r.IsPassed);
        var failedTests = totalTests - passedTests;
        var totalPoints = _testResults.Sum(r => r.Points);
        var earnedPoints = _testResults.Where(r => r.IsPassed).Sum(r => r.Points);
        var percentage = totalPoints > 0 ? (double)earnedPoints / totalPoints * 100 : 0;

        const int w = 64;
        string Line(char c = '─') => new string(c, w);

        _output.AppendLine();
        _output.AppendLine(Color(Bold + Cyan, "╔" + Line('═') + "╗"));
        _output.AppendLine(Color(Bold + Cyan, "║") + Color(Bold, CenterText("🏨  HOTEL APP — TEST RESULTS", w)) + Color(Bold + Cyan, "║"));
        _output.AppendLine(Color(Bold + Cyan, "╚" + Line('═') + "╝"));
        _output.AppendLine($"  {Color(Grey, "Student")}  {Color(Bold, _studentId)}    {Color(Grey, "Run")}  {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

        // ── Failed tests ─────────────────────────────────────
        var failed = _testResults.Where(r => !r.IsPassed).ToList();
        if (failed.Count > 0)
        {
            _output.AppendLine();
            _output.AppendLine(Color(Bold + Red, $"  ✗ FAILED TESTS ({failed.Count})"));
            _output.AppendLine(Color(Grey, "  " + Line()));
            foreach (var r in failed)
            {
                _output.AppendLine($"  {Color(Red, "•")} {Color(Cyan, $"[{r.TestCategory}]")} {r.TestName} {Color(Grey, $"({r.Points} pts)")}");
            }
        }
        else if (totalTests > 0)
        {
            _output.AppendLine();
            _output.AppendLine(Color(Bold + Green, "  ✔ All tests passed!  🎉"));
        }

        // ── Category breakdown ───────────────────────────────
        _output.AppendLine();
        _output.AppendLine(Color(Bold, "  CATEGORY BREAKDOWN"));
        _output.AppendLine(Color(Grey, "  " + Line()));
        foreach (var category in _testResults.Select(r => r.TestCategory).Distinct())
        {
            var ct = _testResults.Where(r => r.TestCategory == category).ToList();
            var cPassed = ct.Count(r => r.IsPassed);
            var cPts = ct.Where(r => r.IsPassed).Sum(r => r.Points);
            var cTotal = ct.Sum(r => r.Points);
            var cPct = cTotal > 0 ? (double)cPts / cTotal * 100 : 0;

            _output.AppendLine($"  {Color(Cyan, $"{category,-14}")} {Bar(cPct)} {Color(Bold, $"{cPct,5:0.#}%")}  " +
                               Color(Grey, $"{cPassed}/{ct.Count} tests · {cPts}/{cTotal} pts"));
        }

        // ── Totals ───────────────────────────────────────────
        var passColor = percentage >= 80 ? Green : percentage >= 50 ? Yellow : Red;
        _output.AppendLine();
        _output.AppendLine(Color(Bold + passColor, "╔" + Line('═') + "╗"));
        _output.AppendLine($"  {Color(Green, $"✔ {passedTests} passed")}    {Color(Red, $"✗ {failedTests} failed")}    {Color(Grey, $"of {totalTests} total")}");
        _output.AppendLine($"  {Color(Bold, "SCORE")}  {Bar(percentage, 28)}");
        _output.AppendLine($"  {Color(Bold + passColor, $"➜  {earnedPoints} / {totalPoints} points   ({percentage:0.#}%)")}");
        _output.AppendLine(Color(Bold + passColor, "╚" + Line('═') + "╝"));
    }

    private static string CenterText(string text, int width)
    {
        // emoji width is approximate; keep simple
        var pad = Math.Max(0, width - text.Length);
        var left = pad / 2;
        return new string(' ', left) + text + new string(' ', pad - left);
    }

    public void FlushOutput()
    {
        // Ensure the box-drawing characters and icons render instead of '?'.
        try { Console.OutputEncoding = Encoding.UTF8; } catch { /* output redirected */ }

        var rendered = _output.ToString();

        // Print to the console (shown only at 'detailed' verbosity, alongside the
        // runner's own noise) ...
        Console.WriteLine(rendered);

        // ... and always save the rendered report to a file so a wrapper script can
        // run the suite quietly and print *only* this clean summary afterwards.
        try
        {
            var summaryPath = Path.Combine(
                Path.GetDirectoryName(_logFilePath) ?? ".",
                "test_summary.txt");
            File.WriteAllText(summaryPath, rendered, new UTF8Encoding(false));
        }
        catch { /* best effort */ }

        _output.Clear();
    }

    public async Task SaveToFileAsync()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(_testResults, options);
        await File.WriteAllTextAsync(_logFilePath, json);

        Console.WriteLine($"\nTest results saved to {_logFilePath}");
    }
}