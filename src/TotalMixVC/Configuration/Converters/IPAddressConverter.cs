using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TotalMixVC.Configuration.Converters;

/// <summary>
/// Converts an IP address string into a IPAddress object.
/// </summary>
public class IPAddressConverter : JsonConverter<IPAddress>
{
    /// <summary>Reads and converts the JSON to an IP address.</summary>
    /// <param name="reader">The reader.</param>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">An object that specifies serialization options to use.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="JsonException">Thrown if value conversion fails.</exception>
    public override IPAddress? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        try
        {
            return IPAddress.Parse(reader.GetString()!);
        }
        catch (FormatException ex)
        {
            throw new JsonException(message: null, innerException: ex);
        }
    }

    /// <summary>Writes a specified value as JSON.</summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="value">The value to convert to JSON.</param>
    /// <param name="options">An object that specifies serialization options to use.</param>
    public override void Write(
        Utf8JsonWriter writer,
        IPAddress value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStringValue(value.ToString());
    }
}
