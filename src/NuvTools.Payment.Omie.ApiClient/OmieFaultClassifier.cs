using NuvTools.Common.ResultWrapper;

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
public static class OmieFaultClassifier
{
    // Omie duplicate cCodIntOS, e.g. "O código de integração [xxx] já foi cadastrado anteriormente."
    private static readonly string[] DuplicateMarkers =
    [
        "cadastrado anteriormente",
        "ja foi cadastrado",
        "já foi cadastrado",
        "ja cadastrado",
        "já cadastrado"
    ];

    // Omie not-found, e.g. "OS não encontrada" / "não localizada" / "not found".
    private static readonly string[] NotFoundMarkers =
    [
        "nao encontrad",
        "não encontrad",
        "nao localizad",
        "não localizad",
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

    private static bool ContainsAny(string? message, string[] markers)
    {
        if (string.IsNullOrWhiteSpace(message)) return false;
        foreach (var marker in markers)
            if (message.Contains(marker, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
}
