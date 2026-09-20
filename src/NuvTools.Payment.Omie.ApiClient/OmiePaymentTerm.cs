namespace NuvTools.Payment.Omie.ApiClient;

/// <summary>
/// The payment term code (<c>cCodParc</c>) of a service order in Omie. It is not a preference: it tells Omie
/// whether the installments come from a registered payment condition or from the request itself.
/// </summary>
public static class OmiePaymentTerm
{
    /// <summary>
    /// Installments defined by the integrator (<c>999</c>) — the only code under which Omie accepts the
    /// <c>Parcelas</c> structure. Any service order that sends an explicit installment must use this code.
    /// <para>
    /// Sending another code alongside the installments passes <c>IncluirOS</c> — Omie stores the order already
    /// with <c>999</c> — but <c>AlterarOS</c> refuses it: "A estrutura de parcelas [Parcelas] está disponível
    /// apenas para o Código de Parcelamento [cCodParc=999]!". An order included that way only fails on its first
    /// change, much later.
    /// </para>
    /// </summary>
    public const string CustomInstallments = "999";
}
