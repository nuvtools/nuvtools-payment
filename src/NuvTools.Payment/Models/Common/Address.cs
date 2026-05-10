namespace NuvTools.Payment.Models.Common;

/// <summary>
/// Postal address used by neutral payment DTOs. Fields capture the common subset
/// across providers — provider-specific extras (number, complement, country) stay on
/// the provider DTOs.
/// </summary>
public class Address
{
    public string? Street { get; set; }

    public string? District { get; set; }

    public string? City { get; set; }

    /// <summary>Two-letter Brazilian state code (UF), e.g. "SP".</summary>
    public string? State { get; set; }

    /// <summary>Postal code (CEP). Digits only or formatted "00000-000" — provider mappers normalize.</summary>
    public string? PostalCode { get; set; }
}
