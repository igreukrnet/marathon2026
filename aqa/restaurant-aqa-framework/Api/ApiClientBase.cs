using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Configuration;

namespace Restaurant_Aqa_Framework.Api;

/// <summary>
/// Base for per-resource API clients. Always builds a fully-qualified URL from
/// <see cref="Settings.ApiBaseUrl"/> rather than trusting the request context's own base URL —
/// a `@ui` scenario's <c>ICallTheApi</c> ability is backed by the browser context's
/// <c>APIRequest</c>, whose base URL is the UI origin, not the API origin.
/// </summary>
public abstract class ApiClientBase(IAPIRequestContext api)
{
    private const int MaxAttempts = 3;

    protected IAPIRequestContext Api { get; } = api;

    // The shared "develop" backend answers an occasional transient 500 ({"message": "Internal server
    // error"} from API Gateway/Lambda). Idempotent calls are retried a couple of times so that noise
    // doesn't fail an unrelated scenario; POST/PATCH are not retried (they could create/mutate twice),
    // and every attempt is logged so real flakiness stays visible.
    protected Task<IAPIResponse> GetAsync(string url, APIRequestContextOptions? options = null) =>
        SendWithRetryAsync("GET", url, options, () => Api.GetAsync(url, options));

    protected Task<IAPIResponse> PostAsync(string url, APIRequestContextOptions? options = null) =>
        SendLoggedAsync("POST", url, options, () => Api.PostAsync(url, options));

    protected Task<IAPIResponse> PutAsync(string url, APIRequestContextOptions? options = null) =>
        SendWithRetryAsync("PUT", url, options, () => Api.PutAsync(url, options));

    protected Task<IAPIResponse> PatchAsync(string url, APIRequestContextOptions? options = null) =>
        SendLoggedAsync("PATCH", url, options, () => Api.PatchAsync(url, options));

    protected Task<IAPIResponse> DeleteAsync(string url, APIRequestContextOptions? options = null) =>
        SendWithRetryAsync("DELETE", url, options, () => Api.DeleteAsync(url, options));

    private static Task<IAPIResponse> SendLoggedAsync(string method, string url, APIRequestContextOptions? options, Func<Task<IAPIResponse>> send) =>
        SendCoreAsync(method, url, options, send, retryOnServerError: false);

    private static Task<IAPIResponse> SendWithRetryAsync(string method, string url, APIRequestContextOptions? options, Func<Task<IAPIResponse>> send) =>
        SendCoreAsync(method, url, options, send, retryOnServerError: true);

    private static async Task<IAPIResponse> SendCoreAsync(
        string method,
        string url,
        APIRequestContextOptions? options,
        Func<Task<IAPIResponse>> send,
        bool retryOnServerError)
    {
        var maxAttempts = retryOnServerError ? MaxAttempts : 1;
        for (var attempt = 1; ; attempt++)
        {
            var sw = Stopwatch.StartNew();
            IAPIResponse? response = null;
            Exception? error = null;
            try
            {
                response = await send();
                sw.Stop();
                var willRetry = retryOnServerError && response.Status >= 500 && attempt < maxAttempts;
                ApiCallLogger.Record(await BuildLogEntryAsync(method, url, options, response, null, sw.Elapsed, attempt, maxAttempts));
                Console.WriteLine($"[ApiClient] {method} {ApiCallLogger.SanitizeUrl(url)} -> {response.Status} ({sw.Elapsed.TotalMilliseconds:F0} ms, attempt {attempt}/{maxAttempts})");
                if (!willRetry)
                    return response;

                Console.Error.WriteLine($"[ApiClient] {method} {ApiCallLogger.SanitizeUrl(url)} answered {response.Status}; retrying ({attempt}/{maxAttempts - 1}).");
                await Task.Delay(TimeSpan.FromMilliseconds(300 * attempt));
            }
            catch (Exception ex)
            {
                sw.Stop();
                error = ex;
                ApiCallLogger.Record(await BuildLogEntryAsync(method, url, options, null, error, sw.Elapsed, attempt, maxAttempts));
                Console.WriteLine($"[ApiClient] {method} {ApiCallLogger.SanitizeUrl(url)} -> ERROR ({sw.Elapsed.TotalMilliseconds:F0} ms, attempt {attempt}/{maxAttempts})");
                throw;
            }
        }
    }

    private static async Task<string> BuildLogEntryAsync(
        string method,
        string url,
        APIRequestContextOptions? options,
        IAPIResponse? response,
        Exception? error,
        TimeSpan elapsed,
        int attempt,
        int maxAttempts)
    {
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"[{DateTime.UtcNow:O}] {method} {ApiCallLogger.SanitizeUrl(url)} (attempt {attempt}/{maxAttempts}, {elapsed.TotalMilliseconds:F0} ms)");
        sb.AppendLine("Request headers:");
        sb.AppendLine(Indent(ApiCallLogger.FormatHeaders(options?.Headers)));
        sb.AppendLine("Request body:");
        sb.AppendLine(Indent(ApiCallLogger.FormatBody(options?.DataString)));

        if (error is not null)
        {
            sb.AppendLine("Error:");
            sb.AppendLine(Indent(error.GetType().Name + ": " + error.Message));
            return sb.ToString().TrimEnd();
        }

        if (response is not null)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"Response: {response.Status} {response.StatusText}");
            sb.AppendLine("Response headers:");
            sb.AppendLine(Indent(ApiCallLogger.FormatHeaders(response.Headers)));
            sb.AppendLine("Response body:");
            string body;
            try
            {
                body = await response.TextAsync();
            }
            catch (Exception ex)
            {
                body = $"<failed to read body: {ex.Message}>";
            }
            sb.AppendLine(Indent(ApiCallLogger.FormatBody(body)));
        }

        return sb.ToString().TrimEnd();
    }

    private static string Indent(string value) =>
        string.Join(Environment.NewLine, value.Split(Environment.NewLine).Select(l => "  " + l));

    protected static string Url(string path, IReadOnlyDictionary<string, string?>? query = null)
    {
        var url = Settings.Current.ApiBaseUrl.TrimEnd('/') + path;
        if (query is null || query.Count == 0)
            return url;

        var pairs = query
            .Where(kv => kv.Value is not null)
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}");
        return url + "?" + string.Join("&", pairs);
    }

    protected static string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonDefaults.Options);

    /// <summary>Builds request headers for a JSON body, optionally bearing a Cognito access/id token.
    /// Several write endpoints (dishes/locations/tables) have no gateway auth at all, so the token is
    /// optional everywhere — passing one is harmless where it's ignored.</summary>
    protected static Dictionary<string, string> Headers(string? bearerToken = null)
    {
        var headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" };
        if (!string.IsNullOrEmpty(bearerToken))
            headers["Authorization"] = $"Bearer {bearerToken}";
        return headers;
    }
}
