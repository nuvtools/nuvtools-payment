namespace NuvTools.Payment.Omie.ApiClient;

/// <summary>
/// The service order stage (<c>cEtapa</c>) in Omie. The API documents five values — 10, 20, 30, 40 and 50 (invoice)
/// — but an account may have its own Kanban columns, and the query returns the real stage (60 has been seen).
/// Echoing that stage back on a change means sending a value outside the domain the API accepts; since
/// <c>cEtapa</c> is optional on AlterarOS, the right move is to omit it and leave the order where it is.
/// </summary>
public static class OmieOrderStage
{
    private static readonly string[] Documented = ["10", "20", "30", "40", "50"];

    /// <summary>The stage to send on a change: the current one when the API knows it, or none.</summary>
    public static string? ForChange(string? currentStage)
        => currentStage is not null && Documented.Contains(currentStage) ? currentStage : null;

    /// <summary>Default item action on a change ("A" — alter), required by AlterarOS on every item.</summary>
    public const string AlterItemAction = "A";
}
