﻿using System.Text.Json;
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
        var json = $$"""{"VolumeFineIncrement": {{volumeFineIncrement}}}""";

        // Act
        var model = JsonSerializer.Deserialize<Model>(json);

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
        var json = $$"""{"VolumeFineIncrement": {{volumeFineIncrement}}}""";

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
        var model = new Model() { VolumeFineIncrement = volumeFineIncrement };

        // Act
        var json = JsonSerializer.Serialize(model);

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
        var model = new Model() { VolumeFineIncrement = volumeFineIncrement };

        // Act
        Action action = () => JsonSerializer.Serialize(model);

        // Assert
        Assert.Throws<JsonException>(action);
    }

    internal sealed record Model
    {
        [JsonConverter(typeof(VolumeFineIncrementFloatConverter))]
        public required float VolumeFineIncrement { get; init; }
    }
}
