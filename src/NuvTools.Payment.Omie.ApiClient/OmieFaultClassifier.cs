using System.Text.RegularExpressions;
using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.Omie.ApiClient.Resources;

namespace NuvTools.Payment.Omie.ApiClient;

/// <summary>
/// Classifies Omie failure messages so callers can react programmatically (e.g. switch an OS create to an
/// update when the integration code already exists) instead of string-matching Omie text themselves. This
/// component surfaces faults as failed <see cref="IResult"/> (it deliberately does not throw — see the
/// comment on <c>OmieApiClient._staticClient</c>), so the markers live here in one place.
///
/// VERIFY (Omie test app): confirm the exact faultstring wording for the duplicate-integration-code and
/// not-found cases against the current Omie responses and adjust the markers below if needed.
/// </summary>
public static partial class OmieFaultClassifier
{
    // Omie consumption control, not a business error: the same call with the same parameters arriving again within
    // roughly a minute (REDUNDANT), or the app key blocked for excessive calls (MISUSE_API_PROCESS).
    private static readonly string[] RedundantConsumptionMarkers =
    [
        "REDUNDANT",
        "consumo redundante",
        "redundant consumption"
    ];

    private static readonly string[] BlockedConsumptionMarkers =
    [
        "MISUSE_API_PROCESS",
        "consumo indevido"
    ];

    // Omie duplicate cCodIntOS, e.g. "O código de integração [xxx] já foi cadastrado anteriormente."
    private static readonly string[] DuplicateMarkers =
    [
        "cadastrado anteriormente",
        "ja foi cadastrado",
        "já foi cadastrado",
        "ja cadastrado",
        "já cadastrado"
    ];

    // Omie not-found, e.g. "OS não encontrada" / "não localizada" / "não cadastrada" / "inexistente" / "not found".
    private static readonly string[] NotFoundMarkers =
    [
        "nao encontrad",
        "não encontrad",
        "nao localizad",
        "não localizad",
        "nao cadastrad",
        "não cadastrad",
        "inexistente",
        "not found",
        "nenhum registro"
    ];

    // AlterarOS: "Informe a Tag [nCodOS] ou [cCodIntOS] na alteração!". Observed with BOTH tags filled inside
    // cabecalho — where the API documents them — on a payload Omie had accepted minutes earlier, unchanged. The
    // wording blames the caller; the evidence says otherwise, so callers treat it as a transient refusal.
    private static readonly string[] MissingOrderIdentificationMarkers =
    [
        "informe a tag"
    ];

    /// <summary>True when the failure indicates the integration code (cCodIntOS) already exists in Omie.</summary>
    public static bool IsDuplicateIntegrationCode(string? message)
        => ContainsAny(message, DuplicateMarkers);

    /// <summary>
    /// True when AlterarOS refused the change claiming the OS identification tags were not informed. Intermittent:
    /// the same body, byte for byte, has been accepted and refused by Omie minutes apart. Retrying later is the
    /// only remedy — but not immediately, since an identical repeated call is what Omie punishes as redundant
    /// consumption.
    /// </summary>
    public static bool IsMissingOrderIdentification(string? message)
        => ContainsAny(message, MissingOrderIdentificationMarkers)
            && (message!.Contains("nCodOS", StringComparison.OrdinalIgnoreCase)
                || message.Contains("cCodIntOS", StringComparison.OrdinalIgnoreCase));

    /// <summary>True when the failure indicates the requested record was not found in Omie.</summary>
    public static bool IsNotFound(string? message)
        => ContainsAny(message, NotFoundMarkers);

    /// <summary>Convenience overload over a failed <see cref="IResult"/>.</summary>
    public static bool IsDuplicateIntegrationCode(IResult result)
        => result is not null && !result.Succeeded && IsDuplicateIntegrationCode(result.Message);

    /// <summary>Convenience overload over a failed <see cref="IResult"/>.</summary>
    public static bool IsNotFound(IResult result)
        => result is not null && !result.Succeeded && IsNotFound(result.Message);

    /// <summary>Convenience overload over a failed <see cref="IResult"/>.</summary>
    public static bool IsMissingOrderIdentification(IResult result)
        => result is not null && !result.Succeeded && IsMissingOrderIdentification(result.Message);

    /// <summary>
    /// True when Omie refused the call to control consumption rather than because of the request itself — either a
    /// redundant call (the same parameters again within about a minute) or an app key temporarily blocked for
    /// excessive calls. Neither is fixed by changing the request: only by waiting.
    /// </summary>
    public static bool IsThrottled(string? message)
        => ContainsAny(message, RedundantConsumptionMarkers) || ContainsAny(message, BlockedConsumptionMarkers);

    /// <summary>Convenience overload over a failed <see cref="IResult"/>.</summary>
    public static bool IsThrottled(IResult result)
        => result is not null && !result.Succeeded && IsThrottled(result.Message);

    /// <summary>
    /// A message for the end user about a throttled call, preserving the wait Omie reported when there is one.
    /// Telling someone to wait is a different instruction from telling them to fix something, which is why the
    /// refusal is worth distinguishing.
    /// </summary>
    public static string DescribeThrottle(string? message)
    {
        var seconds = SecondsPattern().Match(message ?? string.Empty);

        var wait = seconds.Success
            ? string.Format(Messages.OmieWaitXSecondsAndRetry, seconds.Groups[1].Value)
            : Messages.OmieWaitAndRetry;

        return ContainsAny(message, BlockedConsumptionMarkers)
            ? $"{Messages.OmieBlockedForExcessiveCalls} {wait}"
            : $"{Messages.OmieRefusedRedundantCall} {wait}";
    }

    private static bool ContainsAny(string? message, string[] markers)
    {
        if (string.IsNullOrWhiteSpace(message)) return false;
        foreach (var marker in markers)
            if (message.Contains(marker, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    /// <summary>"Tente novamente em 1322 segundos" / "wait 59 seconds" — the number Omie returns.</summary>
    [GeneratedRegex(@"(\d+)\s*(?:seconds?|segundos?)", RegexOptions.IgnoreCase)]
    private static partial Regex SecondsPattern();
}
