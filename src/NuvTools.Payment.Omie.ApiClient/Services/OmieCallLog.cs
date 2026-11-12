using System.Text.RegularExpressions;

namespace NuvTools.Payment.Omie.ApiClient.Services;

/// <summary>
/// Prepares an Omie call for the log of a failed request: the request body exactly as it was sent, with only the
/// credentials (<c>app_key</c>, <c>app_secret</c>) replaced by <see cref="Mask"/>, and the call name read from the
/// envelope. Text-level masking on purpose — re-serializing the JSON would reorder or reformat it, and the point of
/// the log is to show what actually left the client.
/// </summary>
internal static partial class OmieCallLog
{
    /// <summary>What replaces each credential value in the logged request.</summary>
    internal const string Mask = "***";

    /// <summary>
    /// Upper bound for each logged body. An OS with many items stays well below it; the cap only keeps an unexpected
    /// payload (an HTML error page, a huge listing) from flooding the log.
    /// </summary>
    internal const int MaxBodyLength = 8000;

    /// <summary>The request body with <c>app_key</c> and <c>app_secret</c> values masked, truncated to <see cref="MaxBodyLength"/>.</summary>
    internal static string MaskCredentials(string? json)
        => string.IsNullOrEmpty(json)
            ? string.Empty
            : Truncate(CredentialPattern().Replace(json, $"$1\"{Mask}\""));

    /// <summary>The <c>call</c> of the envelope (e.g. <c>ConsultarOS</c>), or null when it cannot be read.</summary>
    internal static string? CallOf(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;

        var match = CallPattern().Match(json);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>The value cut at <see cref="MaxBodyLength"/> characters, marked when cut.</summary>
    internal static string Truncate(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        return value.Length <= MaxBodyLength ? value : value[..MaxBodyLength] + "…";
    }

    [GeneratedRegex("(\"app_(?:key|secret)\"\\s*:\\s*)\"(?:[^\"\\\\]|\\\\.)*\"", RegexOptions.IgnoreCase)]
    private static partial Regex CredentialPattern();

    [GeneratedRegex("\"call\"\\s*:\\s*\"([^\"]*)\"", RegexOptions.IgnoreCase)]
    private static partial Regex CallPattern();
}
