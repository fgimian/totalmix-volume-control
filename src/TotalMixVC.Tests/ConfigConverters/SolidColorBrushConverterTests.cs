using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media;
using TotalMixVC.Configuration.Converters;
using Xunit;

namespace TotalMixVC.Tests.ConfigConverters;

public class SolidColorBrushConverterTests
{
    [Theory]
    [InlineData("#fff", "#FFFFFFFF")]
    [InlineData("#442266", "#FF442266")]
    [InlineData("#33FFaa99", "#33FFAA99")]
    [InlineData("red", "#FFFF0000")]
    [InlineData("BLUE", "#FF0000FF")]
    public void Read_Valid_ConvertsWithoutError(string color, string expected)
    {
        // Arrange
        var json = $$"""{"Color": "{{color}}"}""";

        // Act
        var model = JsonSerializer.Deserialize<Model>(json);

        // Assert
        Assert.Equal(expected, model?.Color.ToString(CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("ffffff")]
    [InlineData("#yyxxzz")]
    public void Read_Invalid_ThrowsException(string color)
    {
        // Arrange
        var json = $$"""{"Color": "{{color}}"}""";

        // Act
        Action action = () => JsonSerializer.Deserialize<Model>(json);

        // Assert
        Assert.Throws<JsonException>(action);
    }

    [Theory]
    [InlineData("#ff00ff", "#ff00ff")]
    [InlineData("#33ff00ff", "#33ff00ff")]
    [InlineData("#fff", "#ffffff")]
    public void Write_Valid_ConvertsWithoutError(string color, string expected)
    {
        // Arrange
        var converter = new BrushConverter();
        var model = new Model() { Color = (SolidColorBrush)converter.ConvertFromString(color)! };

        // Act
        var json = JsonSerializer.Serialize(model);

        // Assert
        Assert.Equal($$"""{"Color":"{{expected}}"}""", json);
    }

    internal sealed record Model
    {
        [JsonConverter(typeof(SolidColorBrushConverter))]
        public required SolidColorBrush Color { get; init; }
    }
}
