namespace NuvTools.Payment.DTOs.Requests;

/// <summary>
/// What a payee does, as the provider asks it to assess the account. Not identity data: none of it is
/// collected through a token.
/// </summary>
/// <remarks>Every field is optional; whatever is left out stays as it was.</remarks>
/// <param name="Mcc">The merchant category code, four digits.</param>
/// <param name="Url">The payee's site or public profile.</param>
/// <param name="ProductDescription">What they sell, for a payee without a site.</param>
/// <param name="MonthlyEstimatedRevenueMinor">Their estimated monthly revenue, in minor units of <paramref name="Currency"/>.</param>
/// <param name="Currency">ISO 4217 of the revenue estimate.</param>
public record PayeeBusinessProfile(
    string? Mcc = null,
    string? Url = null,
    string? ProductDescription = null,
    long? MonthlyEstimatedRevenueMinor = null,
    string? Currency = null);
