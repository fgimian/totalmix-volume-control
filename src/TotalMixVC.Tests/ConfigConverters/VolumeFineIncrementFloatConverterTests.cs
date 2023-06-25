using System.Text.Json;
using System.Text.Json.Serialization;
using TotalMixVC.Configuration.Converters;
using Xunit;

namespace TotalMixVC.Tests.ConfigConverters;

public class VolumeFineIncrementFloatConverterTests
{
    [Theory]
    [InlineData(0.01)]
    [InlineData(0.03)]
    [InlineData(0.05)]
    public void Read_Valid_ConvertsWithoutError(float volumeFineIncrement)
    {
        // Arrange
        string json = $$"""{"VolumeFineIncrement": {{volumeFineIncrement}}}""";

        // Act
        Model? model = JsonSerializer.Deserialize<Model>(json);

        // Assert
        Assert.Equal(volumeFineIncrement, model?.VolumeFineIncrement);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.06)]
    [InlineData(1.0)]
    [InlineData(-1.0)]
    public void Read_Invalid_ThrowsException(float volumeFineIncrement)
    {
        // Arrange
        string json = $$"""{"VolumeFineIncrement": {{volumeFineIncrement}}}""";

        // Act
        Action action = () => JsonSerializer.Deserialize<Model>(json);

        // Assert
        Assert.Throws<JsonException>(action);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.03)]
    [InlineData(0.05)]
    public void Write_Valid_ConvertsWithoutError(float volumeFineIncrement)
    {
        // Arrange
        Model model = new() { VolumeFineIncrement = volumeFineIncrement };

        // Act
        string json = JsonSerializer.Serialize(model);

        // Assert
        Assert.Equal($$"""{"VolumeFineIncrement":{{volumeFineIncrement}}}""", json);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.06)]
    [InlineData(1.0)]
    [InlineData(-1.0)]
    public void Write_Invalid_ThrowsException(float volumeFineIncrement)
    {
        // Arrange
        Model model = new() { VolumeFineIncrement = volumeFineIncrement };

        // Act
        Action action = () => JsonSerializer.Serialize(model);

        // Assert
        Assert.Throws<JsonException>(action);
    }

    internal record Model
    {
        [JsonConverter(typeof(VolumeFineIncrementFloatConverter))]
        public required float VolumeFineIncrement { get; init; }
    }
}
