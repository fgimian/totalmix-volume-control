using System.Text.Json;
using System.Text.Json.Serialization;
using TotalMixVC.Configuration.Converters;
using Xunit;

namespace TotalMixVC.Tests.ConfigConverters;

public class PositiveDoubleConverterTests
{
    [Theory]
    [InlineData(0.01)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(10.0)]
    public void Read_Valid_ConvertsWithoutError(double value)
    {
        // Arrange
        var json = $$"""{"Value": {{value}}}""";

        // Act
        var model = JsonSerializer.Deserialize<Model>(json);

        // Assert
        Assert.Equal(value, model?.Value);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.5)]
    [InlineData(-1.0)]
    [InlineData(-10.5)]
    public void Read_Invalid_ThrowsException(double value)
    {
        // Arrange
        var json = $$"""{"Value": {{value}}}""";

        // Act
        Action action = () => JsonSerializer.Deserialize<Model>(json);

        // Assert
        Assert.Throws<JsonException>(action);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(10.0)]
    public void Write_Valid_ConvertsWithoutError(double value)
    {
        // Arrange
        var model = new Model() { Value = value };

        // Act
        var json = JsonSerializer.Serialize(model);

        // Assert
        Assert.Equal($$"""{"Value":{{value}}}""", json);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.5)]
    [InlineData(-1.0)]
    [InlineData(-10.5)]
    public void Write_Invalid_ThrowsException(double value)
    {
        // Arrange
        var model = new Model() { Value = value };

        // Act
        Action action = () => JsonSerializer.Serialize(model);

        // Assert
        Assert.Throws<JsonException>(action);
    }

    internal sealed record Model
    {
        [JsonConverter(typeof(PositiveDoubleConverter))]
        public required double Value { get; init; }
    }
}
