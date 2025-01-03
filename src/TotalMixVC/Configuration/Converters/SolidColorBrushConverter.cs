﻿using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace TotalMixVC.Configuration.Converters;

/// <summary>
/// Converts a hex color string into a SolidColorBrush object.
/// </summary>
public class SolidColorBrushConverter : JsonConverter<SolidColorBrush>
{
    /// <summary>Reads and converts the JSON to a solid color brush.</summary>
    /// <param name="reader">The reader.</param>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">An object that specifies serialization options to use.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="JsonException">Thrown if value conversion fails.</exception>
    public override SolidColorBrush? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        var converter = new BrushConverter();
        try
        {
            return (SolidColorBrush?)converter.ConvertFromString(reader.GetString()!);
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
        SolidColorBrush value,
        JsonSerializerOptions options
    )
    {
        var hexColor = value.Color.ToString(CultureInfo.InvariantCulture);
        if (hexColor.Length == 9 && hexColor.Substring(1, 2) == "FF")
        {
            hexColor = hexColor.Remove(1, 2);
        }

        writer.WriteStringValue(hexColor.ToLowerInvariant());
    }
}
