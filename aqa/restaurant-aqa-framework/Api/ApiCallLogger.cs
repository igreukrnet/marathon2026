using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Restaurant_Aqa_Framework.Api;

/// <summary>
/// Collects per-async-flow API call logs so they can be attached to Allure / test reports
/// without coupling tests to a specific reporting framework. Each scenario runs in its own
/// async context; <see cref="AsyncLocal{T}"/> keeps logs isolated.
/// </summary>
public static class ApiCallLogger
{
    private const int MaxBodyChars = 8 * 1024;

    private static readonly AsyncLocal<List<string>?> Current = new();

    private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization", "Proxy-Authorization", "Cookie", "Set-Cookie", "X-Api-Key", "Api-Key"
    };

    private static readonly HashSet<string> SensitiveJsonKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "confirmPassword", "token", "idToken", "accessToken", "refreshToken",
        "sessionToken", "authorization", "secret", "apiKey", "key"
    };

    private static readonly HashSet<string> SensitiveQueryKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "token", "access_token", "id_token", "sessionToken", "key", "apiKey", "password", "code"
    };

    private static readonly JsonSerializerOptions PrettyJsonOptions = new() { WriteIndented = true };

    public static void BeginScope() => Current.Value = new List<string>();

    public static void Clear() => Current.Value = null;

    public static IReadOnlyList<string> SnapshotCurrent() => Current.Value is { Count: > 0 } list ? [.. list] : [];

    public static void Record(string entry)
    {
        var list = Current.Value;
        if (list is null)
        {
            list = new List<string>();
            Current.Value = list;
        }

        list.Add(entry);
    }

    public static string SanitizeUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url;

        if (string.IsNullOrEmpty(uri.Query))
            return url;

        var builder = new UriBuilder(uri);
        var query = builder.Query.TrimStart('?');
        if (string.IsNullOrEmpty(query))
            return url;

        var parts = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < parts.Length; i++)
        {
            var kv = parts[i].Split('=', 2);
            var key = Uri.UnescapeDataString(kv[0]);
            if (SensitiveQueryKeys.Contains(key))
                parts[i] = $"{kv[0]}=***";
        }

        builder.Query = string.Join("&", parts);
        return builder.Uri.ToString();
    }

    public static string FormatHeaders(IEnumerable<KeyValuePair<string, string>>? headers)
    {
        if (headers is null)
            return "(none)";

        var items = headers
            .Select(h => $"{h.Key}: {(SensitiveHeaders.Contains(h.Key) ? "***" : h.Value)}")
            .ToList();

        return items.Count == 0 ? "(none)" : string.Join(Environment.NewLine, items);
    }

    public static string FormatHeaders(IEnumerable<KeyValuePair<string, IEnumerable<string>>>? headers)
    {
        if (headers is null)
            return "(none)";

        var items = headers
            .Select(h => $"{h.Key}: {(SensitiveHeaders.Contains(h.Key) ? "***" : string.Join(", ", h.Value))}")
            .ToList();

        return items.Count == 0 ? "(none)" : string.Join(Environment.NewLine, items);
    }

    public static string FormatBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return "(empty)";

        var text = body.Trim();
        try
        {
            using var doc = JsonDocument.Parse(text);
            var masked = MaskJsonElement(doc.RootElement);
            text = JsonSerializer.Serialize(masked, PrettyJsonOptions);
        }
        catch
        {
            // Not JSON - keep as-is but still mask obvious token/password patterns.
            text = Regex.Replace(text, "(?i)(\"?(password|token|authorization|idToken|accessToken|refreshToken|sessionToken|apiKey|secret)\"?\\s*[:=]\\s*\")([^\"]*)(\")", "$1***$4");
            text = Regex.Replace(text, "(?i)(Bearer\\s+)[A-Za-z0-9\\-\\._~\\+/]+=*", "$1***");
        }

        if (text.Length > MaxBodyChars)
            text = text[..MaxBodyChars] + $"{Environment.NewLine}... [truncated, {body.Length} chars total]";

        return text;
    }

    private static object? MaskJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(
                p => p.Name,
                p => SensitiveJsonKeys.Contains(p.Name) ? "***" : MaskJsonElement(p.Value)),
            JsonValueKind.Array => element.EnumerateArray().Select(MaskJsonElement).ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };
    }
}
