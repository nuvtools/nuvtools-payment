using System.Globalization;
using NuvTools.Payment.BancoDoBrasil.ApiClient.DTOs.Requests;
using NuvTools.Payment.BancoDoBrasil.ApiClient.DTOs.Responses;
using NuvTools.Payment.Models.BatchPayment;
using NuvTools.Payment.Models.Common;

namespace NuvTools.Payment.BancoDoBrasil.ApiClient.Mapping;

internal static class BbBatchPaymentMapper
{
    public static AccessToken ToNeutral(this BbAccessTokenResponse bb)
    {
        return new AccessToken
        {
            Token = bb.AccessToken ?? string.Empty,
            TokenType = bb.TokenType,
            ExpiresIn = bb.ExpiresIn
        };
    }

    public static CreateBatchPaymentRequest ToBb(this BankSlipBatchPaymentRequest neutral)
    {
        return new CreateBatchPaymentRequest
        {
            NumeroRequisicao = neutral.RequestNumber,
            NumeroAgenciaDebito = neutral.DebitAgency,
            NumeroContaCorrenteDebito = neutral.DebitAccount,
            DigitoVerificadorContaCorrenteDebito = neutral.DebitAccountDigit,
            Lancamentos = [.. neutral.Items.Select(i => new BatchPaymentItemRequest
            {
                NumeroSequencialLancamento = i.Sequence,
                CodigoBarras = i.Barcode,
                DataVencimento = i.DueDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                DataPagamento = i.PaymentDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                ValorPagamento = i.PaymentAmount,
                ValorNominal = i.NominalAmount,
                DescricaoPagamento = i.Description
            })]
        };
    }

    public static BankSlipBatchPaymentResult ToNeutral(this CreateBatchPaymentResponse bb)
    {
        return new BankSlipBatchPaymentResult
        {
            RequestState = bb.EstadoRequisicao,
            RequestNumber = bb.NumeroRequisicao,
            ItemCount = bb.QuantidadeLancamentos,
            TotalAmount = bb.ValorLancamentos,
            Items = bb.Lancamentos?.ConvertAll(ToNeutralItem) ?? []
        };
    }

    public static BankSlipBatchPaymentStatus ToNeutral(this BankSlipPaymentResponse bb)
    {
        return new BankSlipBatchPaymentStatus
        {
            RequestState = bb.EstadoRequisicao,
            RequestNumber = bb.NumeroRequisicao,
            ItemCount = bb.QuantidadeLancamentos,
            TotalAmount = bb.ValorLancamentos,
            Items = bb.Lancamentos?.ConvertAll(ToNeutralItem) ?? [],
            Refunds = bb.Devolucoes?.ConvertAll(ToNeutralRefund) ?? []
        };
    }

    private static BankSlipBatchPaymentResultItem ToNeutralItem(BatchPaymentItemResponse bb)
    {
        return new BankSlipBatchPaymentResultItem
        {
            Sequence = bb.NumeroSequencialLancamento,
            Barcode = bb.CodigoBarras,
            State = bb.EstadoLancamento,
            StateDescription = bb.DescricaoEstadoLancamento,
            PaymentAmount = bb.ValorPagamento,
            Errors = bb.Erros
        };
    }

    private static BankSlipBatchPaymentResultItem ToNeutralItem(BankSlipPaymentListItemResponse bb)
    {
        return new BankSlipBatchPaymentResultItem
        {
            Sequence = bb.NumeroSequencialLancamento,
            Barcode = bb.CodigoBarras,
            State = bb.EstadoLancamento,
            StateDescription = bb.DescricaoEstadoLancamento,
            PaymentAmount = bb.ValorPagamento,
            PaymentDate = ParseBbDate(bb.DataPagamento),
            DueDate = ParseBbDate(bb.DataVencimento),
            Description = bb.DescricaoPagamento
        };
    }

    private static BankSlipBatchPaymentRefund ToNeutralRefund(BankSlipRefundResponse bb)
    {
        return new BankSlipBatchPaymentRefund
        {
            ReasonCode = bb.CodigoMotivoDevolucao,
            ReasonDescription = bb.DescricaoMotivoDevolucao,
            Amount = bb.ValorDevolucao,
            Date = ParseBbDate(bb.DataDevolucao)
        };
    }

    private static DateOnly? ParseBbDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return DateOnly.TryParseExact(value, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d
            : DateOnly.TryParse(value, out var fallback) ? fallback : null;
    }
}
