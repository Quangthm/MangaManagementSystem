using MangaManagementSystem.Application.Common;
using Xunit;

namespace MangaManagementSystem.Application.UnitTests.Mappers;

public class AssistantTaskRateMapperTests
{
    [Theory]
    [InlineData("SHADING", 100_000)]
    [InlineData("CLEANUP", 80_000)]
    [InlineData("BACKGROUND", 150_000)]
    [InlineData("EFFECTS", 120_000)]
    [InlineData("DIALOGUE", 90_000)]
    [InlineData("TYPESETTING", 70_000)]
    [InlineData("REVIEW", 100_000)]
    public void GetRate_ReturnsKnownRates(string taskType, decimal expectedRate)
    {
        var rate = AssistantTaskRateMapper.GetRate(taskType);
        Assert.Equal(expectedRate, rate);
    }

    [Fact]
    public void GetRate_ReturnsDefault_ForUnknownType()
    {
        var rate = AssistantTaskRateMapper.GetRate("UNKNOWN_TASK_TYPE");
        Assert.Equal(100_000m, rate);
    }

    [Fact]
    public void GetRate_IsCaseInsensitive()
    {
        var rateLower = AssistantTaskRateMapper.GetRate("shading");
        var rateUpper = AssistantTaskRateMapper.GetRate("SHADING");
        var rateMixed = AssistantTaskRateMapper.GetRate("Shading");

        Assert.Equal(100_000m, rateLower);
        Assert.Equal(rateLower, rateUpper);
        Assert.Equal(rateUpper, rateMixed);
    }

    [Theory]
    [InlineData("SHADING", 100_000)]
    [InlineData("UNKNOWN", 100_000)]
    public void GetEstimatedAmount_UsesDefaultRate_WhenCompensationNull(string taskType, decimal expected)
    {
        var amount = AssistantTaskRateMapper.GetEstimatedAmount(taskType, null);
        Assert.Equal(expected, amount);
    }

    [Theory]
    [InlineData("SHADING", 50_000, 50_000)]
    [InlineData("UNKNOWN", 200_000, 200_000)]
    public void GetEstimatedAmount_UsesProvidedCompensation(string taskType, decimal compensation, decimal expected)
    {
        var amount = AssistantTaskRateMapper.GetEstimatedAmount(taskType, compensation);
        Assert.Equal(expected, amount);
    }
}
