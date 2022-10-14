using Prometheus;

namespace Allegro.Prometheus.TrueRpsMetric;

/// <summary>
/// Configuration used by <see cref="TrueRpsMetricFactory"/>.
/// </summary>
public class TrueRpsMetricConfiguration
{
    /// <summary>
    /// Should collect the true RPS metric?
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Collector registry to be used for true RPS metric
    /// </summary>
    public CollectorRegistry Registry { get; set; } = Metrics.DefaultRegistry;

    /// <summary>
    /// Minimal interval for publishing metric
    /// </summary>
    public TimeSpan MinPublishInterval { get; set; } = TimeSpan.FromSeconds(59);
}