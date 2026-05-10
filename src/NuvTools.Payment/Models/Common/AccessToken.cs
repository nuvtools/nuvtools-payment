namespace NuvTools.Payment.Models.Common;

/// <summary>
/// OAuth-style access token returned by a payment provider.
/// </summary>
public class AccessToken
{
    public string Token { get; set; } = string.Empty;

    public string? TokenType { get; set; }

    /// <summary>Token lifetime in seconds.</summary>
    public int ExpiresIn { get; set; }
}
