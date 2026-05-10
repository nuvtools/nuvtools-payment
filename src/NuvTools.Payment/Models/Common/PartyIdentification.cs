namespace NuvTools.Payment.Models.Common;

/// <summary>
/// Identification document for a party in a payment operation (Brazilian CPF or CNPJ).
/// </summary>
public class PartyIdentification
{
    /// <summary>Document digits only (11 for CPF, 14 for CNPJ). Provider mappers strip formatting.</summary>
    public string Document { get; set; } = string.Empty;

    public DocumentType Type { get; set; }
}

/// <summary>Brazilian person/company document type.</summary>
public enum DocumentType
{
    Unknown = 0,
    Individual = 1,
    Company = 2
}
