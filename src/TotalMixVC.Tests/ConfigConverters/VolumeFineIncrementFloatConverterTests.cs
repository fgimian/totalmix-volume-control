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
        var json = $$"""{"VolumeFineIncrement": {{volumeFineIncrement}}}""";
        var model = JsonSerializer.Deserialize<Model>(json);
        Assert.Equal(volumeFineIncrement, model?.VolumeFineIncrement);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.06)]
    [InlineData(1.0)]
    [InlineData(-1.0)]
    public void Read_Invalid_ThrowsException(float volumeFineIncrement)
    {
        var json = $$"""{"VolumeFineIncrement": {{volumeFineIncrement}}}""";
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Model>(json));
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.03)]
    [InlineData(0.05)]
    public void Write_Valid_ConvertsWithoutError(float volumeFineIncrement)
    {
        var model = new Model() { VolumeFineIncrement = volumeFineIncrement };
        var json = JsonSerializer.Serialize(model);
        Assert.Equal($$"""{"VolumeFineIncrement":{{volumeFineIncrement}}}""", json);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.06)]
    [InlineData(1.0)]
    [InlineData(-1.0)]
    public void Write_Invalid_ThrowsException(float volumeFineIncrement)
    {
        var model = new Model() { VolumeFineIncrement = volumeFineIncrement };
        Assert.Throws<JsonException>(() => JsonSerializer.Serialize(model));
    }

    internal sealed record Model
    {
        [JsonConverter(typeof(VolumeFineIncrementFloatConverter))]
        public required float VolumeFineIncrement { get; init; }
    }
}
