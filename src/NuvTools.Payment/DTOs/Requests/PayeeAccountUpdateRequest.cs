namespace NuvTools.Payment.DTOs.Requests;

/// <summary>
/// Changes to a payee account, the identity and bank details among them only as provider tokens.
/// </summary>
/// <remarks>
/// <para>
/// <b>The application never holds the details themselves.</b> The payee's browser sends them straight
/// to the provider with the publishable key and gets a token back; the application passes the token
/// on. Identity numbers, birth dates, addresses and bank accounts therefore never reach its servers,
/// its logs or its database.
/// </para>
/// <para>
/// The acceptance of the provider's terms travels inside the account token too: the provider records
/// it with the address and browser of the person who accepted, which is the record it asks for.
/// </para>
/// </remarks>
public record PayeeAccountUpdateRequest
{
    /// <summary>The legal entity's details and the terms acceptance, as a provider account token.</summary>
    public string? AccountToken { get; init; }

    /// <summary>The bank account to pay out to, as a provider bank-account token.</summary>
    public string? ExternalAccountToken { get; init; }

    /// <summary>What the payee does; set directly, since it is not identity data.</summary>
    public PayeeBusinessProfile? BusinessProfile { get; init; }
}
