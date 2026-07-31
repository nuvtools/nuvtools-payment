using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NuvTools.Payment.Omie.ApiClient.Json;

/// <summary>
/// Reads as text a field Omie may send as text or as a number. Status codes and the NFS-e number are documented as
/// <c>string</c>, but arrive sometimes as <c>"0"</c> and sometimes as <c>0</c> depending on the call — and a
/// <see cref="JsonException"/> here would bring down the whole response read over one field's formatting.
/// </summary>
public sealed class OmieFlexibleStringConverter : JsonConverter<string?>
{
    /// <inheritdoc />
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.TryGetInt64(out var number)
                ? number.ToString(CultureInfo.InvariantCulture)
                : reader.GetDecimal().ToString(CultureInfo.InvariantCulture),
            JsonTokenType.Null => null,
            _ => null
        };

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        => writer.WriteStringValue(value);
}
