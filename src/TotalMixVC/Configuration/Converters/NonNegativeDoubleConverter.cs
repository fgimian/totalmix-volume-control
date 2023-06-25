using System.Text.Json;
using System.Text.Json.Serialization;

namespace TotalMixVC.Configuration.Converters;

/// <summary>
/// Validates that a specified double is non-negative.
/// </summary>
public class NonNegativeDoubleConverter : JsonConverter<double>
{
    /// <summary>Reads and converts the JSON to a non-negative double.</summary>
    /// <param name="reader">The reader.</param>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">An object that specifies serialization options to use.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="JsonException">Thrown if value range validation fails.</exception>
    public override double Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        double value = reader.GetDouble();
        Validate(value);
        return value;
    }

    /// <summary>Writes a specified value as JSON.</summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="value">The value to convert to JSON.</param>
    /// <param name="options">An object that specifies serialization options to use.</param>
    /// <exception cref="JsonException">Thrown if value range validation fails.</exception>
    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
    {
        Validate(value);
        writer.WriteNumberValue(value);
    }

    private static void Validate(double value)
    {
        if (value >= 0)
        {
            return;
        }

        throw new JsonException(
            message: null,
            innerException: new InvalidOperationException(
                "Specified number must be greater than or equal to 0."
            )
        );
    }
}
