using System.Text.Json;
using System.Text.Json.Serialization;
using TotalMixVC.Configuration.Converters;
using Xunit;

namespace TotalMixVC.Tests.ConfigConverters;

public class VolumeMaxFloatConverterTests
{
    [Theory]
    [InlineData(0.01)]
    [InlineData(0.30)]
    [InlineData(0.50)]
    [InlineData(1.00)]
    public void Read_Valid_ConvertsWithoutError(float volumeMax)
    {
        // Arrange
        string json = $$"""{"VolumeMax": {{volumeMax}}}""";

        // Act
        Model? model = JsonSerializer.Deserialize<Model>(json);

        // Assert
        Assert.Equal(volumeMax, model?.VolumeMax);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.01)]
    [InlineData(10.0)]
    [InlineData(-10.0)]
    public void Read_Invalid_ThrowsException(float volumeMax)
    {
        // Arrange
        string json = $$"""{"VolumeMax": {{volumeMax}}}""";

        // Act
        Action action = () => JsonSerializer.Deserialize<Model>(json);

        // Assert
        Assert.Throws<JsonException>(action);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.30)]
    [InlineData(0.50)]
    [InlineData(1.00)]
    public void Write_Valid_ConvertsWithoutError(float volumeMax)
    {
        // Arrange
        Model model = new() { VolumeMax = volumeMax };

        // Act
        string json = JsonSerializer.Serialize(model);

        // Assert
        Assert.Equal($$"""{"VolumeMax":{{volumeMax}}}""", json);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.01)]
    [InlineData(10.0)]
    [InlineData(-10.0)]
    public void Write_Invalid_ThrowsException(float volumeMax)
    {
        // Arrange
        Model model = new() { VolumeMax = volumeMax };

        // Act
        Action action = () => JsonSerializer.Serialize(model);

        // Assert
        Assert.Throws<JsonException>(action);
    }

    internal record Model
    {
        [JsonConverter(typeof(VolumeMaxFloatConverter))]
        public required float VolumeMax { get; init; }
    }
}
