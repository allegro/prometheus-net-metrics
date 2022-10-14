using FluentAssertions;
using Xunit;

namespace Allegro.Prometheus.TrueRpsMetric.Tests;

/// <summary>
/// These are low-level tests of MaxPerSecondGauge's internal state that uses cyclic buffer to track metric value
/// over last second in 10x100ms buckets.
/// </summary>
public class MaxPerSecondGaugeTests
{
    [Fact]
    public void ShouldTrackMaxPerSecondValueInBuckets()
    {
        // arrange
        var sut = new MaxPerSecondGauge.MaxPerSecondGaugeGaugeCollector.MaxPerSecondState(
            new DateTime(2022, 05, 08, 19, 01, 30, 000));

        // act
        sut.Observe(new DateTime(2022, 05, 08, 19, 01, 30, 000), 3);
        sut.Observe(new DateTime(2022, 05, 08, 19, 01, 30, 100), 2);
        sut.Observe(new DateTime(2022, 05, 08, 19, 01, 30, 200), 1);
        sut.Observe(new DateTime(2022, 05, 08, 19, 01, 30, 500), 5);
        sut.Observe(new DateTime(2022, 05, 08, 19, 01, 30, 900), 2.5);

        // assert
        sut.MaxPerSecondSoFar.Should().Be(13.5);
        sut.GetBucketsOrdered().Should().BeEquivalentTo(new[] { 3, 2, 1, 0, 0, 5, 0, 0, 0, 2.5 });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1.5)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void ShouldResetCyclicBufferAfterPeriodLongerThanOneSecond(double seconds)
    {
        // arrange
        var sut = new MaxPerSecondGauge.MaxPerSecondGaugeGaugeCollector.MaxPerSecondState(
            new DateTime(2022, 05, 08, 19, 01, 30, 000));
        sut.Observe(new DateTime(2022, 05, 08, 19, 01, 30, 000), 3);
        sut.Observe(new DateTime(2022, 05, 08, 19, 01, 30, 100), 2);
        sut.Observe(new DateTime(2022, 05, 08, 19, 01, 30, 200), 1);
        sut.Observe(new DateTime(2022, 05, 08, 19, 01, 30, 500), 5);
        sut.Observe(new DateTime(2022, 05, 08, 19, 01, 30, 900), 2.5);
        sut.Observe(new DateTime(2022, 05, 08, 19, 01, 31, 150), 5);

        // act
        sut.Observe(new DateTime(2022, 05, 08, 19, 01, 31, 150).AddSeconds(seconds), 10);

        // assert
        sut.GetBucketsOrdered().Should().BeEquivalentTo(new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 10 });
    }
}