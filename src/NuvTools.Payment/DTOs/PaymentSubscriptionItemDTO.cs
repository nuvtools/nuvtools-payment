namespace NuvTools.Payment.DTOs;

/// <summary>One price a subscription is charged at, and how many of it.</summary>
/// <param name="PriceLookupKey">The lookup key of the price, or <see langword="null"/> when the price has none.</param>
/// <param name="Quantity">How many units are charged — the seats of a per-seat price, 1 for a flat one.</param>
public record PaymentSubscriptionItemDTO(string? PriceLookupKey, long Quantity);
