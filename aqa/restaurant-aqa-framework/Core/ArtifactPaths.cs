namespace Restaurant_Aqa_Framework.Core;

/// <summary>
/// Single home for everything a test run produces, so a run's output is not
/// scattered across the framework. Layout (all under <see cref="Root"/>):
/// <code>
/// TestResults/
///   *-report.html, *.trx, *.json   TUnit report / TRX (--report-trx; also the re-run-failed TRX files)
///   reqnroll/                      Reqnroll HTML report + messages ndjson (see reqnroll.json)
///   traces/                        Playwright trace zips (npx playwright show-trace &lt;file&gt;)
///   screenshots/                   UI screenshots captured per scenario
///   allure-results/                Raw Allure results (JSON/attachments)
///   allure-report/                 Generated Allure HTML report (CI/local generate step)
/// </code>
/// The root is the project folder's `TestResults/` locally (found by walking up from the
/// binaries to the `.csproj`) and falls back to `TestResults/` next to the binaries when
/// no project is found (e.g. a published build). Override with the ARTIFACTS_DIR env var.
/// </summary>
public static class ArtifactPaths
{
    public static string Root { get; } = ResolveRoot();

    public static string Traces => Directory.CreateDirectory(Path.Combine(Root, "traces")).FullName;

    public static string Screenshots => Directory.CreateDirectory(Path.Combine(Root, "screenshots")).FullName;

    public static string AllureResults => Directory.CreateDirectory(Path.Combine(Root, "allure-results")).FullName;

    public static string AllureReport => Path.Combine(Root, "allure-report");

    /// <summary>Removes traces left by earlier runs, so the folder only ever holds the current run's.</summary>
    public static void ClearTraces()
    {
        var traces = Path.Combine(Root, "traces");
        if (Directory.Exists(traces))
        {
            try
            {
                Directory.Delete(traces, recursive: true);
            }
            catch
            {
                // Ignore transient delete issues
            }
        }
        Directory.CreateDirectory(traces);
    }

    /// <summary>Removes Allure raw results from earlier runs to avoid mixing test executions.</summary>
    public static void ClearAllureResults()
    {
        var results = Path.Combine(Root, "allure-results");
        if (Directory.Exists(results))
        {
            try
            {
                var dir = new DirectoryInfo(results);
                foreach (var file in dir.GetFiles())
                {
                    try { file.Delete(); } catch { }
                }
                foreach (var sub in dir.GetDirectories())
                {
                    try { sub.Delete(true); } catch { }
                }
            }
            catch
            {
                // Ignore transient delete issues
            }
        }
        Directory.CreateDirectory(results);
    }

    private static string ResolveRoot()
    {
        var overrideDir = Environment.GetEnvironmentVariable("ARTIFACTS_DIR");
        if (!string.IsNullOrWhiteSpace(overrideDir))
            return Path.GetFullPath(overrideDir);

        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (dir.GetFiles("*.csproj").Length > 0)
                return Path.Combine(dir.FullName, "TestResults");
        }

        return Path.Combine(AppContext.BaseDirectory, "TestResults");
    }
}
