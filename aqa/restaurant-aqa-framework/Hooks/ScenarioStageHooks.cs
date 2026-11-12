using System.Text;
using Allure.Net.Commons;
using Microsoft.Playwright;
using Reqnroll.BoDi;
using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Seeding;
using Restaurant_Aqa_Framework.Configuration;
using Restaurant_Aqa_Framework.Contexts;
using Restaurant_Aqa_Framework.Core;
using Restaurant_Aqa_Framework.Screenplay;
using Restaurant_Aqa_Framework.Screenplay.Abilities;

namespace Restaurant_Aqa_Framework.Hooks;

/// <summary>
/// Builds the per-scenario stage (`@ui` or `@api`) and registers it — plus the
/// `Actor` it hands abilities to — into the scenario's own `IObjectContainer`,
/// so step definitions get them via constructor injection. Never uses
/// `ScenarioContext.Current`/static/`ThreadStatic` state: Reqnroll throws on
/// the static accessor under parallel execution by design.
/// </summary>
[Binding]
public class ScenarioStageHooks(IObjectContainer container, ScenarioContext scenario)
{
    [BeforeScenario("ui", "api")]
    public static void BeginApiLogScope()
    {
        ApiCallLogger.BeginScope();
    }

    [BeforeScenario("ui")]
    public async Task SetUpUiStage()
    {
        var settings = Settings.Current;
        var browserContext = await PlaywrightRunHooks.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = settings.UiBaseUrl,
        });
        browserContext.SetDefaultTimeout(settings.DefaultTimeoutMs);
        browserContext.SetDefaultNavigationTimeout(settings.NavigationTimeoutMs);
        await browserContext.Tracing.StartAsync(new TracingStartOptions { Screenshots = true, Snapshots = true, Sources = true });
        var page = await browserContext.NewPageAsync();

        var browseTheWeb = new BrowseTheWeb(browserContext, page);
        var actor = Actor.Named("Customer")
            .WhoCan<IBrowseTheWeb>(browseTheWeb)
            .WhoCan<ICallTheApi>(browseTheWeb)
            .WhoCan<IRemember>(new RememberThings(scenario));
        Equip(actor);

        container.RegisterInstanceAs<IStageContext>(new UiStageContext(browserContext, actor));
        container.RegisterInstanceAs(actor);
    }

    [BeforeScenario("api")]
    public async Task SetUpApiStage()
    {
        var settings = Settings.Current;
        var apiRequestContext = await PlaywrightRunHooks.Playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
        {
            BaseURL = settings.ApiBaseUrl,
            Timeout = settings.DefaultTimeoutMs,
        });

        var actor = Actor.Named("ApiClient")
            .WhoCan<ICallTheApi>(new CallTheApi(apiRequestContext))
            .WhoCan<IRemember>(new RememberThings(scenario));
        Equip(actor);

        container.RegisterInstanceAs<IStageContext>(new ApiStageContext(apiRequestContext, actor));
        container.RegisterInstanceAs(actor);
    }

    /// <summary>What every scenario's actor starts out remembering: the scenario's last API response (shared with
    /// the step definitions) and the scope that removes whatever the scenario creates.</summary>
    private void Equip(Actor actor)
    {
        actor.Remember(container.Resolve<ApiResponseContext>());
        actor.Remember(new CleanupScope());
    }

    /// <summary>Removes everything the scenario created, once, for every step class. Runs before the stage is
    /// torn down (Order -100; <see cref="TearDownStage"/> uses the default) because the removal still needs the
    /// stage's request context.</summary>
    [AfterScenario("ui", "api", Order = -100)]
    public async Task CleanUpScenarioData()
    {
        if (container.IsRegistered<Actor>() && container.Resolve<Actor>().TryRecall<CleanupScope>(out var cleanup))
            await cleanup.DisposeAsync();
    }

    [AfterScenario("ui", "api", Order = 100)]
    public async Task TearDownStage(ScenarioContext scenarioContext, IReqnrollOutputHelper outputHelper)
    {
        var stage = container.Resolve<IStageContext>();

        try
        {
            AttachApiLogsToAllure();

            var failed = scenarioContext.TestError is not null and not TUnit.Core.Exceptions.SkipTestException;
            string? tracePath = null;

            if (stage is UiStageContext ui)
            {
                try
                {
                    var pages = ui.BrowserContext.Pages;
                    var page = pages.Count > 0 ? pages[^1] : null;
                    if (page is not null)
                    {
                        var title = SanitizeTitle(scenarioContext.ScenarioInfo.Title);
                        var screenshotPath = Path.Combine(ArtifactPaths.Screenshots, $"{title}.png");
                        await page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = true });
                        outputHelper.AddAttachment(screenshotPath);

                        if (File.Exists(screenshotPath))
                        {
                            var attachmentName = failed ? "UI screenshot on failure" : "UI screenshot";
                            AllureApi.AddAttachment(attachmentName, "image/png", screenshotPath);
                        }
                    }
                }
                catch
                {
                    // Ignore screenshot capture failure to ensure trace and teardown proceed
                }

                tracePath = await HandleTrace(ui.BrowserContext, scenarioContext, outputHelper);
                if (failed && tracePath is not null && File.Exists(tracePath))
                    AllureApi.AddAttachment("Playwright trace on failure", "application/zip", tracePath);
            }
        }
        finally
        {
            await stage.DisposeAsync();
        }
    }

    private static void AttachApiLogsToAllure()
    {
        var logs = ApiCallLogger.SnapshotCurrent();
        if (logs.Count == 0)
            return;

        var sb = new StringBuilder();
        for (var i = 0; i < logs.Count; i++)
        {
            if (i > 0) sb.AppendLine().AppendLine(new string('-', 60));
            sb.Append(logs[i]);
        }

        try
        {
            AllureApi.AddAttachment("API request/response logs", "text/plain", Encoding.UTF8.GetBytes(sb.ToString()), ".log");
        }
        catch
        {
            // Do not fail scenario teardown if Allure attachment fails.
        }
    }

    private static async Task<string?> HandleTrace(IBrowserContext browserContext, ScenarioContext scenarioContext, IReqnrollOutputHelper outputHelper)
    {
        // A skipped scenario surfaces as an exception too, but it did not fail.
        var failed = scenarioContext.TestError is not null and not TUnit.Core.Exceptions.SkipTestException;
        var alwaysTrace = Settings.Current.TraceMode == TraceMode.Always
            || scenarioContext.ScenarioInfo.Tags.Contains("trace-always");

        if (Settings.Current.TraceMode == TraceMode.Never || !(failed || alwaysTrace))
        {
            await browserContext.Tracing.StopAsync();
            return null;
        }

        var title = SanitizeTitle(scenarioContext.ScenarioInfo.Title);
        var status = failed ? "failed" : "passed";
        var path = Path.Combine(ArtifactPaths.Traces, $"{status}-{title}-{Guid.NewGuid():N}.zip");
        await browserContext.Tracing.StopAsync(new TracingStopOptions { Path = path });
        outputHelper.AddAttachment(path);
        return path;
    }

    private static string SanitizeTitle(string title)
    {
        var cleaned = string.Concat(title.Select(c => char.IsLetterOrDigit(c) ? c : '-')).Trim('-');
        return cleaned.Length > 60 ? cleaned[..60] : cleaned;
    }
}
