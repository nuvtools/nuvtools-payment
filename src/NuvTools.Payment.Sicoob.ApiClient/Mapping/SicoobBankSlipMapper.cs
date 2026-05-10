using NuvTools.Payment.Models.BankSlip;
using NuvTools.Payment.Models.Common;
using NuvTools.Payment.Sicoob.ApiClient.DTOs.Requests;
using NuvTools.Payment.Sicoob.ApiClient.DTOs.Responses;

namespace NuvTools.Payment.Sicoob.ApiClient.Mapping;

internal static class SicoobBankSlipMapper
{
    public static CreateBankSlipRequest ToSicoob(this BankSlipCreateRequest neutral)
    {
        ArgumentNullException.ThrowIfNull(neutral.DueDate);
        ArgumentNullException.ThrowIfNull(neutral.IssueDate);
        ArgumentNullException.ThrowIfNull(neutral.Payer);

        return new CreateBankSlipRequest
        {
            NumeroCliente = neutral.Reference.ClientNumber,
            CodigoModalidade = neutral.Reference.ModalityCode,
            NossoNumero = neutral.Reference.OurNumber,
            SeuNumero = neutral.YourNumber,
            DataVencimento = neutral.DueDate.Value.ToString("yyyy-MM-dd"),
            DataEmissao = neutral.IssueDate.Value.ToString("yyyy-MM-dd"),
            Valor = neutral.Amount,
            TipoJurosMora = MapChargeType(neutral.Charge?.InterestType),
            ValorJurosMora = neutral.Charge?.InterestValue ?? 0m,
            TipoMulta = MapChargeType(neutral.Charge?.FineType),
            ValorMulta = neutral.Charge?.FineValue ?? 0m,
            TipoDesconto = MapDiscountType(neutral.Discount?.Type),
            ValorDesconto = neutral.Discount?.Value ?? 0m,
            DataDesconto = neutral.Discount?.LimitDate?.ToString("yyyy-MM-dd"),
            NumeroParcela = neutral.InstallmentNumber,
            Aceite = neutral.Accept,
            Pagador = neutral.Payer.ToSicoob()
        };
    }

    public static CreateBankSlipPayerRequest ToSicoob(this BankSlipPayer neutral)
    {
        return new CreateBankSlipPayerRequest
        {
            NumeroCpfCnpj = neutral.Identification.Document,
            Nome = neutral.Name,
            Endereco = neutral.Address?.Street,
            Bairro = neutral.Address?.District,
            Cidade = neutral.Address?.City,
            Cep = neutral.Address?.PostalCode,
            Uf = neutral.Address?.State,
            Email = neutral.Email
        };
    }

    public static BankSlip ToNeutral(this BankSlipResponse sicoob)
    {
        return new BankSlip
        {
            Reference = new BankSlipReference
            {
                ClientNumber = sicoob.NumeroCliente,
                ModalityCode = sicoob.CodigoModalidade,
                OurNumber = sicoob.NossoNumero
            },
            YourNumber = sicoob.SeuNumero,
            DueDate = ParseDate(sicoob.DataVencimento),
            Amount = sicoob.Valor,
            RebateAmount = sicoob.ValorAbatimento,
            Status = MapStatus(sicoob.SituacaoBoleto),
            DigitableLine = sicoob.LinhaDigitavel,
            Barcode = sicoob.CodigoBarras,
            Payer = sicoob.Pagador?.ToNeutral()
        };
    }

    public static BankSlip ToNeutral(this CreateBankSlipResponse sicoob, BankSlipReference reference)
    {
        return new BankSlip
        {
            Reference = new BankSlipReference
            {
                ClientNumber = reference.ClientNumber,
                ModalityCode = reference.ModalityCode,
                OurNumber = sicoob.NossoNumero
            },
            DigitableLine = sicoob.LinhaDigitavel,
            Barcode = sicoob.CodigoBarras,
            Status = MapStatus(sicoob.SituacaoBoleto)
        };
    }

    public static BankSlipSecondCopy ToNeutral(this SecondCopyBankSlipResponse sicoob, BankSlipReference reference)
    {
        return new BankSlipSecondCopy
        {
            Reference = new BankSlipReference
            {
                ClientNumber = reference.ClientNumber,
                ModalityCode = reference.ModalityCode,
                OurNumber = sicoob.NossoNumero
            },
            DigitableLine = sicoob.LinhaDigitavel,
            Barcode = sicoob.CodigoBarras,
            PdfBase64 = sicoob.PdfBoleto
        };
    }

    public static BankSlipPayer ToNeutral(this BankSlipPayerResponse sicoob)
    {
        var document = sicoob.NumeroCpfCnpj ?? string.Empty;
        return new BankSlipPayer
        {
            Identification = new PartyIdentification
            {
                Document = document,
                Type = InferDocumentType(document)
            },
            Name = sicoob.Nome,
            Address = new Address
            {
                Street = sicoob.Endereco,
                District = sicoob.Bairro,
                City = sicoob.Cidade,
                PostalCode = sicoob.Cep,
                State = sicoob.Uf
            }
        };
    }

    private static int MapChargeType(BankSlipChargeType? type) => type switch
    {
        BankSlipChargeType.None => 0,
        BankSlipChargeType.FixedValue => 1,
        BankSlipChargeType.PercentageOnNominalValue => 2,
        BankSlipChargeType.DailyValue => 3,
        _ => 0
    };

    private static int MapDiscountType(BankSlipDiscountType? type) => type switch
    {
        BankSlipDiscountType.None => 0,
        BankSlipDiscountType.FixedValue => 1,
        BankSlipDiscountType.PercentageOnNominalValue => 2,
        _ => 0
    };

    private static BankSlipStatus MapStatus(int sicoobStatus) => sicoobStatus switch
    {
        1 => BankSlipStatus.Open,
        2 => BankSlipStatus.Paid,
        3 => BankSlipStatus.Settled,
        4 => BankSlipStatus.Canceled,
        5 => BankSlipStatus.Overdue,
        6 => BankSlipStatus.Protested,
        _ => BankSlipStatus.Unknown
    };

    private static DateOnly? ParseDate(string? value)
        => DateOnly.TryParse(value, out var d) ? d : null;

    private static DocumentType InferDocumentType(string document)
    {
        var digits = document.Length;
        return digits switch
        {
            11 => DocumentType.Individual,
            14 => DocumentType.Company,
            _ => DocumentType.Unknown
        };
    }
}
