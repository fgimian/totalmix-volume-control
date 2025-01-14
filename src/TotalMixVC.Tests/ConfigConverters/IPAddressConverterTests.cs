using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using TotalMixVC.Configuration.Converters;
using Xunit;

namespace TotalMixVC.Tests.ConfigConverters;

public class IPAddressConverterTests
{
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("172.16.0.1")]
    [InlineData("192.168.0.1")]
    public void Read_Valid_ConvertsWithoutError(string address)
    {
        var json = $$"""{"Address": "{{address}}"}""";
        var model = JsonSerializer.Deserialize<Model>(json);
        Assert.Equal(address, model?.Address.ToString());
    }

    [Theory]
    [InlineData("abc127.0.0.1")]
    [InlineData("172.16abc.0.1")]
    [InlineData("example.com")]
    [InlineData("osc.example.com")]
    public void Read_Invalid_ThrowsException(string address)
    {
        var json = $$"""{"Address": "{{address}}"}""";
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Model>(json));
    }

    [Fact]
    public void Write_Valid_ConvertsWithoutError()
    {
        var model = new Model() { Address = IPAddress.Loopback };
        var json = JsonSerializer.Serialize(model);
        /*lang=json,strict*/
        Assert.Equal("""{"Address":"127.0.0.1"}""", json);
    }

    internal sealed record Model
    {
        [JsonConverter(typeof(IPAddressConverter))]
        public required IPAddress Address { get; init; }
    }
}
