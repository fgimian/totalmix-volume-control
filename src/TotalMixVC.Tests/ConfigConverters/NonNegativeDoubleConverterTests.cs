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
        var json = $$"""{"Value": {{value}}}""";
        var model = JsonSerializer.Deserialize<Model>(json);
        Assert.Equal(value, model?.Value);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-0.5)]
    [InlineData(-1.0)]
    [InlineData(-10.5)]
    public void Read_Invalid_ThrowsException(double value)
    {
        var json = $$"""{"Value": {{value}}}""";
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Model>(json));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(10.0)]
    public void Write_Valid_ConvertsWithoutError(double value)
    {
        var model = new Model() { Value = value };
        var json = JsonSerializer.Serialize(model);
        Assert.Equal($$"""{"Value":{{value}}}""", json);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-0.5)]
    [InlineData(-1.0)]
    [InlineData(-10.5)]
    public void Write_Invalid_ThrowsException(double value)
    {
        var model = new Model() { Value = value };
        Assert.Throws<JsonException>(() => JsonSerializer.Serialize(model));
    }

    internal sealed record Model
    {
        [JsonConverter(typeof(NonNegativeDoubleConverter))]
        public required double Value { get; init; }
    }
}
