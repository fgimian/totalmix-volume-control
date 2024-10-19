using System.Text.Json;
using System.Text.Json.Serialization;
using TotalMixVC.Configuration.Converters;
using Xunit;

namespace TotalMixVC.Tests.ConfigConverters;

public class VolumeIncrementFloatConverterTests
{
    [Theory]
    [InlineData(0.01)]
    [InlineData(0.03)]
    [InlineData(0.05)]
    [InlineData(0.10)]
    public void Read_Valid_ConvertsWithoutError(float volumeIncrement)
    {
        // Arrange
        string json = $$"""{"VolumeIncrement": {{volumeIncrement}}}""";

        // Act
        Model? model = JsonSerializer.Deserialize<Model>(json);

        // Assert
        Assert.Equal(volumeIncrement, model?.VolumeIncrement);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.11)]
    [InlineData(1.0)]
    [InlineData(-1.0)]
    public void Read_Invalid_ThrowsException(float volumeIncrement)
    {
        // Arrange
        string json = $$"""{"VolumeIncrement": {{volumeIncrement}}}""";

        // Act
        Action action = () => JsonSerializer.Deserialize<Model>(json);

        // Assert
        Assert.Throws<JsonException>(action);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.03)]
    [InlineData(0.05)]
    [InlineData(0.10)]
    public void Write_Valid_ConvertsWithoutError(float volumeIncrement)
    {
        // Arrange
        Model model = new() { VolumeIncrement = volumeIncrement };

        // Act
        string json = JsonSerializer.Serialize(model);

        // Assert
        Assert.Equal($$"""{"VolumeIncrement":{{volumeIncrement}}}""", json);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.11)]
    [InlineData(1.0)]
    [InlineData(-1.0)]
    public void Write_Invalid_ThrowsException(float volumeIncrement)
    {
        // Arrange
        Model model = new() { VolumeIncrement = volumeIncrement };

        // Act
        Action action = () => JsonSerializer.Serialize(model);

        // Assert
        Assert.Throws<JsonException>(action);
    }

    internal sealed record Model
    {
        [JsonConverter(typeof(VolumeIncrementFloatConverter))]
        public required float VolumeIncrement { get; init; }
    }
}
