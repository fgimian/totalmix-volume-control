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
        var json = $$"""{"VolumeIncrement": {{volumeIncrement}}}""";
        var model = JsonSerializer.Deserialize<Model>(json);
        Assert.Equal(volumeIncrement, model?.VolumeIncrement);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.11)]
    [InlineData(1.0)]
    [InlineData(-1.0)]
    public void Read_Invalid_ThrowsException(float volumeIncrement)
    {
        var json = $$"""{"VolumeIncrement": {{volumeIncrement}}}""";
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Model>(json));
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.03)]
    [InlineData(0.05)]
    [InlineData(0.10)]
    public void Write_Valid_ConvertsWithoutError(float volumeIncrement)
    {
        var model = new Model() { VolumeIncrement = volumeIncrement };
        var json = JsonSerializer.Serialize(model);
        Assert.Equal($$"""{"VolumeIncrement":{{volumeIncrement}}}""", json);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.11)]
    [InlineData(1.0)]
    [InlineData(-1.0)]
    public void Write_Invalid_ThrowsException(float volumeIncrement)
    {
        var model = new Model() { VolumeIncrement = volumeIncrement };
        Assert.Throws<JsonException>(() => JsonSerializer.Serialize(model));
    }

    internal sealed record Model
    {
        [JsonConverter(typeof(VolumeIncrementFloatConverter))]
        public required float VolumeIncrement { get; init; }
    }
}
