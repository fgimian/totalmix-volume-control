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
        var json = $$"""{"VolumeMax": {{volumeMax}}}""";
        var model = JsonSerializer.Deserialize<Model>(json);
        Assert.Equal(volumeMax, model?.VolumeMax);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.01)]
    [InlineData(10.0)]
    [InlineData(-10.0)]
    public void Read_Invalid_ThrowsException(float volumeMax)
    {
        var json = $$"""{"VolumeMax": {{volumeMax}}}""";
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Model>(json));
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.30)]
    [InlineData(0.50)]
    [InlineData(1.00)]
    public void Write_Valid_ConvertsWithoutError(float volumeMax)
    {
        var model = new Model() { VolumeMax = volumeMax };
        var json = JsonSerializer.Serialize(model);
        Assert.Equal($$"""{"VolumeMax":{{volumeMax}}}""", json);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.01)]
    [InlineData(10.0)]
    [InlineData(-10.0)]
    public void Write_Invalid_ThrowsException(float volumeMax)
    {
        var model = new Model() { VolumeMax = volumeMax };
        Assert.Throws<JsonException>(() => JsonSerializer.Serialize(model));
    }

    internal sealed record Model
    {
        [JsonConverter(typeof(VolumeMaxFloatConverter))]
        public required float VolumeMax { get; init; }
    }
}
