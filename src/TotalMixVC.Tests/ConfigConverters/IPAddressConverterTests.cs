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
        // Arrange
        string json = $$"""{"Address": "{{address}}"}""";

        // Act
        Model? model = JsonSerializer.Deserialize<Model>(json);

        // Assert
        Assert.Equal(address, model?.Address.ToString());
    }

    [Theory]
    [InlineData("abc127.0.0.1")]
    [InlineData("172.16abc.0.1")]
    [InlineData("example.com")]
    [InlineData("osc.example.com")]
    public void Read_Invalid_ThrowsException(string address)
    {
        // Arrange
        string json = $$"""{"Address": "{{address}}"}""";

        // Act
        Action action = () => JsonSerializer.Deserialize<Model>(json);

        // Assert
        Assert.Throws<JsonException>(action);
    }

    [Fact]
    public void Write_Valid_ConvertsWithoutError()
    {
        // Arrange
        Model model = new() { Address = IPAddress.Loopback };

        // Act
        string json = JsonSerializer.Serialize(model);

        // Assert
        Assert.Equal("""{"Address":"127.0.0.1"}""", json);
    }

    internal record Model
    {
        [JsonConverter(typeof(IPAddressConverter))]
        public required IPAddress Address { get; init; }
    }
}
