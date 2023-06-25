using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TotalMixVC.Configuration.Converters;

/// <summary>
/// Validates that a specified network port is within the correct range.
/// </summary>
public class PortIntegerConverter : JsonConverter<int>
{
    /// <summary>Reads and converts the JSON to a network port.</summary>
    /// <param name="reader">The reader.</param>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">An object that specifies serialization options to use.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="JsonException">Thrown if value range validation fails.</exception>
    public override int Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        int value = reader.GetInt32();
        Validate(value);
        return value;
    }

    /// <summary>Writes a specified value as JSON.</summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="value">The value to convert to JSON.</param>
    /// <param name="options">An object that specifies serialization options to use.</param>
    /// <exception cref="JsonException">Thrown if value range validation fails.</exception>
    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        Validate(value);
        writer.WriteNumberValue(value);
    }

    private static void Validate(int value)
    {
        if (value is >= IPEndPoint.MinPort and <= IPEndPoint.MaxPort)
        {
            return;
        }

        throw new JsonException(
            message: null,
            innerException: new InvalidOperationException(
                $"Specified port number must be in the inclusive range of {IPEndPoint.MinPort} "
                    + $"to {IPEndPoint.MaxPort}."
            )
        );
    }
}
