using System.Text.Json;
using System.Text.Json.Serialization;

namespace TotalMixVC.Configuration.Converters;

/// <summary>
/// Validates that a specified max volume is within the correct range.
/// </summary>
public class VolumeMaxFloatConverter : JsonConverter<float>
{
    /// <summary>Reads and converts the JSON to a max volume.</summary>
    /// <param name="reader">The reader.</param>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">An object that specifies serialization options to use.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="JsonException">Thrown if value range validation fails.</exception>
    public override float Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        float value = (float)reader.GetDouble();
        Validate(value);
        return value;
    }

    /// <summary>Writes a specified value as JSON.</summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="value">The value to convert to JSON.</param>
    /// <param name="options">An object that specifies serialization options to use.</param>
    /// <exception cref="JsonException">Thrown if value range validation fails.</exception>
    public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
    {
        Validate(value);
        writer.WriteNumberValue(value);
    }

    private static void Validate(float value)
    {
        if (value is > 0.0f and <= 1.0f)
        {
            return;
        }

        throw new JsonException(
            message: null,
            innerException: new InvalidOperationException(
                "Specified max volume must be greater than 0 and less than or equal to 1.0."
            )
        );
    }
}
