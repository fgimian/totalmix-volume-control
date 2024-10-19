using System.Text.Json;
using System.Text.Json.Serialization;
using TotalMixVC.Configuration.Converters;
using Xunit;

namespace TotalMixVC.Tests.ConfigConverters;

public class NonNegativeDoubleConverterTests
{
    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(10.0)]
    public void Read_Valid_ConvertsWithoutError(double value)
    {
        // Arrange
        string json = $$"""{"Value": {{value}}}""";

        // Act
        Model? model = JsonSerializer.Deserialize<Model>(json);

        // Assert
        Assert.Equal(value, model?.Value);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-0.5)]
    [InlineData(-1.0)]
    [InlineData(-10.5)]
    public void Read_Invalid_ThrowsException(double value)
    {
        // Arrange
        string json = $$"""{"Value": {{value}}}""";

        // Act
        Action action = () => JsonSerializer.Deserialize<Model>(json);

        // Assert
        Assert.Throws<JsonException>(action);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(10.0)]
    public void Write_Valid_ConvertsWithoutError(double value)
    {
        // Arrange
        Model model = new() { Value = value };

        // Act
        string json = JsonSerializer.Serialize(model);

        // Assert
        Assert.Equal($$"""{"Value":{{value}}}""", json);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-0.5)]
    [InlineData(-1.0)]
    [InlineData(-10.5)]
    public void Write_Invalid_ThrowsException(double value)
    {
        // Arrange
        Model model = new() { Value = value };

        // Act
        Action action = () => JsonSerializer.Serialize(model);

        // Assert
        Assert.Throws<JsonException>(action);
    }

    internal sealed record Model
    {
        [JsonConverter(typeof(NonNegativeDoubleConverter))]
        public required double Value { get; init; }
    }
}
