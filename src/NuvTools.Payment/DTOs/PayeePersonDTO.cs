namespace NuvTools.Payment.DTOs;

/// <summary>
/// A person registered on a company payee — its representative, an owner, a director, an executive —
/// in what the people managing the company need to tell them apart.
/// </summary>
/// <remarks>
/// Names and roles only. Their identity number, birth date and address stay with the provider, which
/// returns at most whether each was provided.
/// </remarks>
/// <param name="PersonId">The provider's identifier for the person.</param>
/// <param name="FirstName">Given name.</param>
/// <param name="LastName">Family name.</param>
/// <param name="Representative">Whether they act for the company and accepted the provider's terms for it.</param>
/// <param name="Owner">Whether they own part of the company.</param>
/// <param name="Director">Whether they sit on its board.</param>
/// <param name="Executive">Whether they control or manage it.</param>
/// <param name="PercentOwnership">Their share of the company, when they own part of it.</param>
/// <param name="Title">Their position in the company.</param>
public record PayeePersonDTO(
    string PersonId,
    string? FirstName,
    string? LastName,
    bool Representative,
    bool Owner,
    bool Director,
    bool Executive,
    decimal? PercentOwnership,
    string? Title);
