namespace NuvTools.Payment.Omie.ApiClient.DTOs.Responses;

/// <summary>
/// Fiscal state of a service order in Omie, as <c>StatusOS</c> returns it: which stage it sits in, whether it was
/// cancelled, whether it was already invoiced and — when it was — the NFS-e that came out of it.
/// </summary>
/// <param name="OsCode">Omie's internal service order identifier (<c>nCodOS</c>).</param>
/// <param name="OsNumber">Service order number as the user sees it (<c>cNumOS</c>).</param>
/// <param name="Stage">Kanban stage (<c>cEtapa</c>).</param>
/// <param name="IsCancelled">The service order was cancelled in Omie (<c>cCancelada</c>).</param>
/// <param name="IsInvoiced">Invoicing was triggered (<c>cFaturada</c>) — which does not guarantee an authorized invoice.</param>
/// <param name="TotalValue">Service order total (<c>nValorTot</c>).</param>
/// <param name="Invoice">The NFS-e/RPS of the order. Null when it was never invoiced.</param>
public record OmieServiceOrderStatus(
    long OsCode,
    string? OsNumber,
    string? Stage,
    bool IsCancelled,
    bool IsInvoiced,
    decimal TotalValue,
    OmieServiceInvoiceStatus? Invoice);
