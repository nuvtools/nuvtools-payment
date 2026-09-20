using Microsoft.Extensions.Logging;
using NuvTools.Common.ResultWrapper;
using Stripe;

namespace NuvTools.Payment.Stripe.ApiClient.Services;

/// <summary>
/// One call to Stripe, turned into a result.
/// </summary>
/// <remarks>
/// <para>
/// Stripe's SDK reports everything by throwing. A client that let those through would make every
/// caller wrap every call, and a caller that forgot would turn a declined card into a 500.
/// </para>
/// <para>
/// <b>Stripe's own message is passed through.</b> These messages are written for the person paying —
/// "Your card was declined" — and replacing them with a generic sentence would cost the caller the
/// only explanation they have. The exception is logged with its request id, which is what Stripe's
/// support asks for.
/// </para>
/// </remarks>
internal static class StripeCall
{
    public static async Task<IResult<T>> RunAsync<T>(ILogger logger, string operation, Func<Task<T?>> call)
    {
        try
        {
            var value = await call();

            // The message overload is passed explicitly: for T = string, Success(value) would bind to
            // Success(string message) and hand the caller a successful result carrying no data.
            return value is null
                ? Result<T>.FailNotFound($"Stripe returned no {typeof(T).Name} for {operation}.")
                : Result<T>.Success(value, string.Empty);
        }
        catch (StripeException exception)
        {
            logger.LogWarning(exception,
                "Stripe refused {Operation}: {Code} (request {RequestId}).",
                operation, exception.StripeError?.Code, exception.StripeResponse?.RequestId);

            return Result<T>.Fail(exception.StripeError?.Message ?? exception.Message);
        }
    }
}
