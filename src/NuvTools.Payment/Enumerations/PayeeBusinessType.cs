namespace NuvTools.Payment.Enumerations;

/// <summary>Who a payee is in law, which decides what the provider asks of them.</summary>
public enum PayeeBusinessType
{
    /// <summary>A person, paid under their own name and tax identifier.</summary>
    Individual = 1,

    /// <summary>A company, paid under its registered name and tax identifier.</summary>
    Company = 2
}
