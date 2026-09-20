using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.Omie.ApiClient.DTOs.Responses;
using NuvTools.Payment.Omie.ApiClient.Json;
using NuvTools.Payment.Omie.ApiClient.Resources;

namespace NuvTools.Payment.Omie.ApiClient.Services;

/// <summary>
/// Invoicing of a single service order in Omie — the documented path to issue the NFS-e of one OS, which
/// <see cref="Contracts.IOmieApiClient"/> does not cover and therefore goes through the direct client:
/// <list type="number">
/// <item><c>StatusOS</c> (<c>servicos/os/</c>): fiscal state of the order — stage, whether it was cancelled,
/// whether it was already invoiced and the NFS-e that came out of it. It is the query that says whether there is
/// anything to do, and the one that confirms the result afterwards.</item>
/// <item><c>ValidarOS</c> (<c>servicos/osp/</c>): Omie's validation before invoicing. It is what points out, in the
/// API's own words, which required data is missing on the order or on the client registration — instead of finding
/// out through the city hall's rejection, after the invoice was already requested.</item>
/// <item><c>FaturarOS</c> (<c>servicos/osp/</c>): invoices that order. The equivalent of "Faturar Selecionada" in
/// the Omie UI — not to be confused with <c>FaturarLoteOS</c> (<c>servicos/oslote/</c>), which invoices <b>every</b>
/// order of a stage and, triggered for one client, would take the others along.</item>
/// </list>
/// <para>
/// None of them invents data: the invoice content is the order's. What is decided here is only <i>when</i> to ask
/// for the invoice.
/// </para>
/// </summary>
public class OmieServiceOrderBillingProvider(
    OmieDirectApiClient omie,
    ILogger<OmieServiceOrderBillingProvider> logger)
{
    /// <summary>Success on <c>servicos/osp/</c> operations: <c>cCodStatus</c> equal to zero. Above that is an error.</summary>
    private const string SuccessStatusCode = "0";

    /// <summary>
    /// Fiscal state of the order by integration code. Returns <c>null</c> in <c>Data</c> when the order does not
    /// exist in Omie — absence is not a failure: it is the case of whoever has not created the order yet.
    /// </summary>
    public async Task<IResult<OmieServiceOrderStatus?>> GetStatusAsync(
        string integrationCode, CancellationToken cancellationToken)
    {
        var response = await omie.CallAsync<StatusOSResponse>(
            omie.Options.EndpointOrderService,
            "StatusOS",
            new { cCodIntOS = integrationCode },
            cancellationToken);

        if (!response.Succeeded || response.Data is null)
            return OmieFaultClassifier.IsNotFound(response.Message)
                ? Result<OmieServiceOrderStatus?>.Success(data: null)
                : Result<OmieServiceOrderStatus?>.Fail(response.Messages);

        // A response without nCodOS is not an order: Omie answers this way when the integration code matches nothing.
        if (response.Data.OsCode <= 0)
            return Result<OmieServiceOrderStatus?>.Success(data: null);

        return Result<OmieServiceOrderStatus?>.Success(Map(response.Data));
    }

    /// <summary>
    /// Validates the order for invoicing (<c>ValidarOS</c>). A failure carries Omie's own text, which is what knows
    /// the missing required data — passing it through raw is more useful than translating it by approximation.
    /// </summary>
    public Task<IResult> ValidateAsync(long osCode, string integrationCode, CancellationToken cancellationToken)
        => ExecuteAsync("ValidarOS", osCode, integrationCode, cancellationToken);

    /// <summary>Invoices the order (<c>FaturarOS</c>) — this request is what makes Omie issue that order's NFS-e.</summary>
    public Task<IResult> InvoiceAsync(long osCode, string integrationCode, CancellationToken cancellationToken)
        => ExecuteAsync("FaturarOS", osCode, integrationCode, cancellationToken);

    /// <summary>
    /// A <c>servicos/osp/</c> operation addressed to the order. Both identifiers go together because the
    /// documentation asks for both: the internal one (<c>nCodOS</c>) and the integration one (<c>cCodIntOS</c>).
    /// </summary>
    private async Task<IResult> ExecuteAsync(string call, long osCode, string integrationCode, CancellationToken cancellationToken)
    {
        var response = await omie.CallAsync<OsBillingOperationResponse>(
            omie.Options.EndpointOrderServiceBilling,
            call,
            new { nCodOS = osCode, cCodIntOS = integrationCode },
            cancellationToken);

        if (!response.Succeeded)
            return Result.Fail(response.Message ?? string.Format(Messages.OmieCallFailedX, call));

        var status = response.Data?.StatusCode?.Trim();

        // Omie also refuses inside an HTTP 200: cCodStatus other than zero is an error, and cDescStatus says which.
        if (!string.IsNullOrEmpty(status) && status != SuccessStatusCode)
        {
            logger.LogWarning("Omie {Call} refused OS {OsCode} ({IntegrationCode}). Status {Status}: {Description}",
                call, osCode, integrationCode, status, response.Data?.StatusDescription);

            return Result.Fail(string.IsNullOrWhiteSpace(response.Data?.StatusDescription)
                ? string.Format(Messages.OmieRefusedXForOrderXStatusX, call, osCode, status)
                : response.Data!.StatusDescription!);
        }

        logger.LogInformation("Omie {Call} accepted for OS {OsCode} ({IntegrationCode}): {Description}",
            call, osCode, integrationCode, response.Data?.StatusDescription);

        return Result.Success();
    }

    private static OmieServiceOrderStatus Map(StatusOSResponse response)
        => new(
            response.OsCode,
            response.OsNumber,
            response.Stage?.Trim(),
            IsYes(response.Cancelled),
            IsYes(response.Invoiced),
            response.TotalValue ?? 0m,
            MapInvoice(response.RpsList));

    /// <summary>
    /// The NFS-e of the order. When there is more than one RPS (resubmissions, cancellations), the one that already
    /// has a number wins — it is the invoice that actually exists; with no authorized number, the last one wins,
    /// which is the current progress.
    /// </summary>
    private static OmieServiceInvoiceStatus? MapInvoice(List<StatusOSRps>? list)
    {
        if (list is null || list.Count == 0) return null;

        var rps = list.FindLast(r => !string.IsNullOrWhiteSpace(r.InvoiceNumber)) ?? list[^1];

        var messages = (rps.Messages ?? [])
            .Select(m => string.Join(" ", new[] { m.Code?.Trim(), m.Description?.Trim() }
                .Where(part => !string.IsNullOrWhiteSpace(part))))
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Distinct()
            .ToList();

        return new OmieServiceInvoiceStatus(
            string.IsNullOrWhiteSpace(rps.InvoiceNumber) ? null : rps.InvoiceNumber.Trim(),
            rps.RpsStatus?.Trim(),
            rps.BatchStatus?.Trim(),
            string.IsNullOrWhiteSpace(rps.VerificationCode) ? null : rps.VerificationCode.Trim(),
            string.IsNullOrWhiteSpace(rps.InvoiceUrl) ? null : rps.InvoiceUrl.Trim(),
            messages.Count == 0 ? null : string.Join(" | ", messages));
    }

    private static bool IsYes(string? flag)
        => string.Equals(flag?.Trim(), "S", StringComparison.OrdinalIgnoreCase);

    // ----- Raw Omie response -----

    private sealed class StatusOSResponse
    {
        [JsonPropertyName("nCodOS")] public long OsCode { get; set; }
        [JsonPropertyName("cNumOS")] public string? OsNumber { get; set; }
        [JsonPropertyName("cEtapa")] public string? Stage { get; set; }
        [JsonPropertyName("cCancelada")] public string? Cancelled { get; set; }
        [JsonPropertyName("cFaturada")] public string? Invoiced { get; set; }
        [JsonPropertyName("nValorTot")] public decimal? TotalValue { get; set; }
        [JsonPropertyName("ListaRpsNfse")] public List<StatusOSRps>? RpsList { get; set; }
    }

    private sealed class StatusOSRps
    {
        /// <summary>NFS-e number. Documented as text, read tolerantly: it is the data the feature rests on.</summary>
        [JsonPropertyName("nNfse")]
        [JsonConverter(typeof(OmieFlexibleStringConverter))]
        public string? InvoiceNumber { get; set; }

        [JsonPropertyName("cStatusRps")] public string? RpsStatus { get; set; }
        [JsonPropertyName("cStatusLote")] public string? BatchStatus { get; set; }
        [JsonPropertyName("cCodVerif")] public string? VerificationCode { get; set; }
        [JsonPropertyName("cUrlNfse")] public string? InvoiceUrl { get; set; }
        [JsonPropertyName("mensagens")] public List<StatusOSMessage>? Messages { get; set; }
    }

    private sealed class StatusOSMessage
    {
        [JsonPropertyName("cCodigo")] public string? Code { get; set; }
        [JsonPropertyName("cDescricao")] public string? Description { get; set; }
        [JsonPropertyName("cSituacao")] public string? Situation { get; set; }
    }

    private sealed class OsBillingOperationResponse
    {
        [JsonPropertyName("cCodStatus")]
        [JsonConverter(typeof(OmieFlexibleStringConverter))]
        public string? StatusCode { get; set; }

        [JsonPropertyName("cDescStatus")] public string? StatusDescription { get; set; }
    }
}
