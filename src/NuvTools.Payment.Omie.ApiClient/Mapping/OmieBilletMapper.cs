using System.Globalization;
using NuvTools.Payment.Models.BankSlip;
using NuvTools.Payment.Omie.ApiClient.DTOs.Responses;

namespace NuvTools.Payment.Omie.ApiClient.Mapping;

internal static class OmieBilletMapper
{
    public static BankSlipBilletInfo ToNeutral(this GetBilletResponse omie)
    {
        return new BankSlipBilletInfo
        {
            BilletLink = omie.BilletLink,
            BilletNumber = omie.BilletNumber,
            Barcode = omie.Barcode,
            BankNumber = omie.BankNumber,
            IssueDate = ParseOmieDate(omie.BilletIssueDate),
            DueDate = ParseOmieDate(omie.BilletDueDate),
            InterestPercentage = omie.InterestPercentage,
            FinePercentage = omie.FinePercentage
        };
    }

    public static BankSlipBilletInfo ToNeutral(this GenerateBilletResponse omie)
    {
        return new BankSlipBilletInfo
        {
            BilletLink = omie.BilletLink,
            BilletNumber = omie.BilletNumber,
            Barcode = omie.Barcode,
            BankNumber = omie.BankNumber,
            IssueDate = ParseOmieDate(omie.BilletIssueDate),
            DueDate = ParseOmieDate(omie.BilletDueDate),
            InterestPercentage = omie.InterestPercentage,
            FinePercentage = omie.FinePercentage
        };
    }

    private static DateOnly? ParseOmieDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return DateOnly.TryParseExact(value, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d
            : DateOnly.TryParse(value, out var fallback) ? fallback : null;
    }
}
