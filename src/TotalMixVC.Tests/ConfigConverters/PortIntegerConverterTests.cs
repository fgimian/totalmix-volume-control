using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using TotalMixVC.Configuration.Converters;
using Xunit;

namespace TotalMixVC.Tests.ConfigConverters;

public class PortIntegerConverterTests
{
    [Theory]
    [InlineData(IPEndPoint.MinPort)]
    [InlineData(IPEndPoint.MaxPort)]
    [InlineData(9000)]
    [InlineData(7000)]
    public void Read_Valid_ConvertsWithoutError(int port)
    {
        var json = $$"""{"Port": {{port}}}""";
        var model = JsonSerializer.Deserialize<Model>(json);
        Assert.Equal(port, model?.Port);
    }

    [Theory]
    [InlineData(IPEndPoint.MinPort - 1)]
    [InlineData(IPEndPoint.MaxPort + 1)]
    [InlineData(100_000)]
    [InlineData(-50)]
    public void Read_Invalid_ThrowsException(int port)
    {
        var json = $$"""{"Port": {{port}}}""";
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Model>(json));
    }

    [Theory]
    [InlineData(IPEndPoint.MinPort)]
    [InlineData(IPEndPoint.MaxPort)]
    [InlineData(9000)]
    [InlineData(7000)]
    public void Write_Valid_ConvertsWithoutError(int port)
    {
        var model = new Model() { Port = port };
        var json = JsonSerializer.Serialize(model);
        Assert.Equal($$"""{"Port":{{port}}}""", json);
    }

    [Theory]
    [InlineData(IPEndPoint.MinPort - 1)]
    [InlineData(IPEndPoint.MaxPort + 1)]
    [InlineData(100_000)]
    [InlineData(-50)]
    public void Write_Invalid_ThrowsException(int port)
    {
        var model = new Model() { Port = port };
        Assert.Throws<JsonException>(() => JsonSerializer.Serialize(model));
    }

    internal sealed record Model
    {
        [JsonConverter(typeof(PortIntegerConverter))]
        public required int Port { get; init; }
    }
}
