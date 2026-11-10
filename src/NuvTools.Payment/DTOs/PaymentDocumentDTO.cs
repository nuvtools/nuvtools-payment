namespace NuvTools.Payment.DTOs;

/// <summary>A document the provider renders — an invoice or a credit note — fetched for the caller to serve.</summary>
/// <param name="FileName">A name to save it under, from its number.</param>
/// <param name="ContentType">Its media type, <c>application/pdf</c>.</param>
/// <param name="Content">The file.</param>
public record PaymentDocumentDTO(string FileName, string ContentType, byte[] Content);
