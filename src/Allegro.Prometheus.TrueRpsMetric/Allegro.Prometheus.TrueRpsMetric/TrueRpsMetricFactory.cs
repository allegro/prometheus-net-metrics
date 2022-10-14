using Microsoft.Extensions.Options;
using Prometheus;

namespace Allegro.Prometheus.TrueRpsMetric;

/// <summary>
/// Factory for <see cref="MaxPerSecondGauge"/>
/// </summary>
public static class TrueRpsMetricFactory
{
    /// <summary>
    /// Creates the <see cref="MaxPerSecondGauge"/> using given configuration.
    /// </summary>
    /// <param name="configuration">Configuration of the metric</param>
    /// <returns>Created gauge or null, if <see cref="TrueRpsMetricConfiguration.Enabled"/> is not set to true</returns>
    public static MaxPerSecondGauge? Create(IOptions<TrueRpsMetricConfiguration> configuration)
    {
        if (!configuration.Value.Enabled)
        {
            return null;
        }

        return new MaxPerSecondGauge(
            Metrics.WithCustomRegistry(configuration.Value.Registry)
                .CreateGauge(
                    "http_request_max_rps",
                    "The max RPS over last scrapping period.",
                    "controller",
                    "action",
                    "method"),
            configuration.Value.Registry,
            configuration.Value.MinPublishInterval);
    }
}