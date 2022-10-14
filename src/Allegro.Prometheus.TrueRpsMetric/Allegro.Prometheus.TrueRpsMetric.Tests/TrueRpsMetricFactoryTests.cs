using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Allegro.Prometheus.TrueRpsMetric.Tests;

public class TrueRpsMetricFactoryTests
{
    [Fact]
    public void ShouldCreateExpectedMetric()
    {
        // Act
        var sut = TrueRpsMetricFactory.Create(Options.Create(new TrueRpsMetricConfiguration()));

        // Assert
        sut.Should().NotBeNull();
        sut!.Name
            .Should().Be("http_request_max_rps");
        sut!.LabelNames
            .Should().BeEquivalentTo(
                "controller",
                "action",
                "method");
    }

    [Fact]
    public void ShouldReturnNullWhenDisabled()
    {
        // Act
        var sut = TrueRpsMetricFactory.Create(Options.Create(new TrueRpsMetricConfiguration
        {
            Enabled = false
        }));

        // Assert
        sut.Should().BeNull();
    }
}