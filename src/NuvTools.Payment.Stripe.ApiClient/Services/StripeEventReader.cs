using NuvTools.Payment.DTOs;
using NuvTools.Payment.Enumerations;
using Stripe;
using System.Text.Json;

namespace NuvTools.Payment.Stripe.ApiClient.Services;

/// <summary>
/// Turns a verified Stripe event into the few fields a caller acts on.
/// </summary>
/// <remarks>
/// <para>
/// The reading happens here rather than in the caller, deliberately: which field of which payload
/// holds the customer is Stripe's business, and a caller digging it out itself would be coupled to a
/// shape it does not control and cannot version.
/// </para>
/// <para>
/// Values are read by path rather than deserialized into the SDK's models because what is needed is
/// three or four strings, and a model would make this depend on the shape of everything else in the
/// payload.
/// </para>
/// </remarks>
internal static class StripeEventReader
{
    public const string AccountUpdated = "account.updated";
    public const string SetupIntentSucceeded = "setup_intent.succeeded";
    public const string PaymentIntentSucceeded = "payment_intent.succeeded";
    public const string PaymentIntentFailed = "payment_intent.payment_failed";

    public static PaymentEventDTO Read(Event stripeEvent, string payload)
    {
        var type = Type(stripeEvent.Type);
        var objectId = (stripeEvent.Data?.Object as IHasId)?.Id ?? Value(payload, "id");

        return new PaymentEventDTO(
            stripeEvent.Id,
            type,
            stripeEvent.Type,

            // A Connect event names the account it came from; account.updated is about the object
            // itself, which is the account.
            stripeEvent.Account ?? (type == PaymentEventType.PayeeAccountUpdated ? objectId : null),
            objectId,
            Value(payload, "customer"),
            Value(payload, "payment_method"),
            Value(payload, "last_payment_error", "message"),
            payload);
    }

    private static PaymentEventType Type(string? stripeType) => stripeType switch
    {
        AccountUpdated => PaymentEventType.PayeeAccountUpdated,
        SetupIntentSucceeded => PaymentEventType.PaymentMethodSaved,
        PaymentIntentSucceeded => PaymentEventType.PaymentSucceeded,
        PaymentIntentFailed => PaymentEventType.PaymentFailed,
        _ => PaymentEventType.Unknown
    };

    /// <summary>One value out of <c>data.object</c>, by path, or null when it is not there.</summary>
    private static string? Value(string payload, params string[] path)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);

            if (!document.RootElement.TryGetProperty("data", out var data)
                || !data.TryGetProperty("object", out var element)) return null;

            foreach (var name in path)
            {
                if (!element.TryGetProperty(name, out element)) return null;
            }

            // An expanded object rather than an identifier: not a string, and not what the caller
            // asked for.
            return element.ValueKind == JsonValueKind.String ? element.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
