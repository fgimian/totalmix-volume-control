﻿using System.Net;
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
        // Arrange
        var json = $$"""{"Port": {{port}}}""";

        // Act
        var model = JsonSerializer.Deserialize<Model>(json);

        // Assert
        Assert.Equal(port, model?.Port);
    }

    [Theory]
    [InlineData(IPEndPoint.MinPort - 1)]
    [InlineData(IPEndPoint.MaxPort + 1)]
    [InlineData(100_000)]
    [InlineData(-50)]
    public void Read_Invalid_ThrowsException(int port)
    {
        // Arrange
        var json = $$"""{"Port": {{port}}}""";

        // Act
        Action action = () => JsonSerializer.Deserialize<Model>(json);

        // Assert
        Assert.Throws<JsonException>(action);
    }

    [Theory]
    [InlineData(IPEndPoint.MinPort)]
    [InlineData(IPEndPoint.MaxPort)]
    [InlineData(9000)]
    [InlineData(7000)]
    public void Write_Valid_ConvertsWithoutError(int port)
    {
        // Arrange
        var model = new Model() { Port = port };

        // Act
        var json = JsonSerializer.Serialize(model);

        // Assert
        Assert.Equal($$"""{"Port":{{port}}}""", json);
    }

    [Theory]
    [InlineData(IPEndPoint.MinPort - 1)]
    [InlineData(IPEndPoint.MaxPort + 1)]
    [InlineData(100_000)]
    [InlineData(-50)]
    public void Write_Invalid_ThrowsException(int port)
    {
        // Arrange
        var model = new Model() { Port = port };

        // Act
        Action action = () => JsonSerializer.Serialize(model);

        // Assert
        Assert.Throws<JsonException>(action);
    }

    internal sealed record Model
    {
        [JsonConverter(typeof(PortIntegerConverter))]
        public required int Port { get; init; }
    }
}
