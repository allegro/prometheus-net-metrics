using System.Collections.Concurrent;
using Prometheus;

namespace Allegro.Prometheus.TrueRpsMetric;

/// <summary>
/// Wrapper on <see cref="Gauge"/> that collects max value observed over 1-second periods.
/// The value of the metric is reset on every metric collection.
/// <see cref="MaxPerSecondGauge"/> aggregates a collection of <see cref="MaxPerSecondGaugeGaugeCollector"/>
/// per each labels set.
/// </summary>
public class MaxPerSecondGauge : ICollector
{
    private readonly ICollectorRegistry _registry;
    private readonly TimeSpan _minPublishInterval;
    private readonly ConcurrentDictionary<string, MaxPerSecondGaugeGaugeCollector> _collectors = new();
    private readonly Gauge _gauge;

    /// <summary>
    /// Creates new <see cref="MaxPerSecondGauge"/>.
    /// </summary>
    /// <param name="gauge">
    ///     The <see cref="Gauge"/> metric that will be used to collect max per second value.
    /// </param>
    /// <param name="registry">
    ///     The <see cref="CollectorRegistry"/> that will be used for the metric
    /// </param>
    /// <param name="minPublishInterval">
    ///     Minimal interval for publishing metric
    /// </param>
    public MaxPerSecondGauge(
        Gauge gauge,
        ICollectorRegistry registry,
        TimeSpan minPublishInterval)
    {
        _gauge = gauge;
        _registry = registry;
        _minPublishInterval = minPublishInterval;
    }

    public string Name => _gauge.Name;

    public string Help => _gauge.Help;

    public string[] LabelNames => _gauge.LabelNames;

    /// <summary>
    /// Gets or creates the metric collector for given labels set.
    /// </summary>
    /// <param name="labelValues">Labels set</param>
    /// <returns>The collector</returns>
    public MaxPerSecondGaugeGaugeCollector WithLabels(params string[] labelValues)
    {
        var key = string.Join(",", labelValues);
        return _collectors.GetOrAdd(
            key,
            _ => new MaxPerSecondGaugeGaugeCollector(
                _gauge,
                _registry,
                _minPublishInterval,
                labelValues));
    }

    /// <summary>
    /// Wrapper on <see cref="Gauge"/> that collects max value observed over 1-second periods for specific labels set.
    /// The value of the metric is reset on every metric collection.
    /// </summary>
    public class MaxPerSecondGaugeGaugeCollector
    {
        private readonly Gauge _gauge;
        private readonly string[] _labelValues;
        private readonly TimeSpan _minPublishInterval;
        private readonly object _monitor = new();

        private MaxPerSecondState _state = new(DateTime.UtcNow);

        internal MaxPerSecondGaugeGaugeCollector(
            Gauge gauge,
            ICollectorRegistry collectorRegistry,
            TimeSpan minPublishInterval,
            string[] labelValues)
        {
            _gauge = gauge;
            _labelValues = labelValues;
            _minPublishInterval = minPublishInterval;

            collectorRegistry.AddBeforeCollectCallback(BeforeCollectCallback);
        }

        /// <summary>
        /// Observes the metric value.
        /// </summary>
        /// <param name="val">Observed value</param>
        public void Observe(double val = 1D)
        {
            lock (_monitor)
            {
                _state.Observe(DateTime.UtcNow, val);
            }
        }

        private Task BeforeCollectCallback(CancellationToken cancellationToken)
        {
            double toPublish;

            lock (_monitor)
            {
                if (_state.TrackingFor < _minPublishInterval)
                {
                    return Task.CompletedTask;
                }

                toPublish = _state.MaxPerSecondSoFar;
                _state = new MaxPerSecondState(DateTime.UtcNow);
            }

            _gauge.WithLabels(_labelValues).Set(toPublish);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Holds the current state of the max-per-second metric. Tracks the metric value in 100ms periods, using
        /// cyclic buffer. The buffer is shifted (loosing oldest values) as the new data-points arrive.
        /// </summary>
        internal class MaxPerSecondState
        {
            private const int TrackingBucketsCount = 10;

            private double[] _trackingBuckets = new double[TrackingBucketsCount];
            private DateTime _trackingEndDate;
            private ModuloInteger _trackingIndex;

            public MaxPerSecondState(DateTime trackingSince)
            {
                TrackingSince = trackingSince;
                _trackingEndDate = TrackingSince;
            }

            /// <summary>
            /// When did the max-per-second tracking begin.
            /// </summary>
            public DateTime TrackingSince { get; }

            /// <summary>
            /// How long is the max-per-second being tracked.
            /// </summary>
            public TimeSpan TrackingFor => DateTime.UtcNow - TrackingSince;

            /// <summary>
            /// Max per-second value observed so far since tracking begin.
            /// </summary>
            public double MaxPerSecondSoFar { get; private set; }

            /// <summary>
            /// Observes a value in given date. Shifts the cyclic buffer and increments the last bucket.
            /// </summary>
            /// <param name="date">The date the value was observed</param>
            /// <param name="val">The observed value</param>
            public void Observe(DateTime date, double val)
            {
                var shift = (date - _trackingEndDate).TotalSeconds < 1
                    ? (int)((date - _trackingEndDate).TotalSeconds / (1.0 / TrackingBucketsCount))
                    : TrackingBucketsCount;

                if (shift >= TrackingBucketsCount)
                {
                    _trackingBuckets = new double[TrackingBucketsCount];
                    _trackingEndDate = date;
                }
                else
                {
                    for (var i = 0; i < shift; i++)
                    {
                        _trackingBuckets[_trackingIndex] = 0;
                        _trackingIndex++;
                    }

                    _trackingEndDate = _trackingEndDate.AddSeconds((double)shift / TrackingBucketsCount);
                }

                _trackingBuckets[_trackingIndex + TrackingBucketsCount - 1] += val;

                var currentMaxPerSecond = _trackingBuckets.Sum();
                if (currentMaxPerSecond > MaxPerSecondSoFar)
                {
                    MaxPerSecondSoFar = currentMaxPerSecond;
                }
            }

            /// <summary>
            /// Gets the current 100ms buckets ordered from oldest to current.
            /// </summary>
            internal IEnumerable<double> GetBucketsOrdered()
            {
                for (var i = 0; i < TrackingBucketsCount; i++)
                {
                    yield return _trackingBuckets[_trackingIndex + i];
                }
            }

            /// <summary>
            /// Simple int wrapper that limits its values to 0 (inclusive) - <see cref="TrackingBucketsCount"/> (exclusive).
            /// In mathematics it would be called a ring 'Zn', where n equals <see cref="TrackingBucketsCount"/>.
            /// </summary>
            internal struct ModuloInteger
            {
                private int _value;

                private int Value
                {
                    get => _value;
                    set
                    {
                        _value = value % TrackingBucketsCount;
                        while (_value < 0)
                        {
                            _value = TrackingBucketsCount + _value;
                        }
                    }
                }

                public override string ToString()
                {
#pragma warning disable MA0011
                    return _value.ToString();
#pragma warning restore MA0011
                }

                public static implicit operator int(ModuloInteger val) => val._value;

                public static ModuloInteger operator +(ModuloInteger val, int add) =>
                    val with { Value = val.Value + add };

                public static ModuloInteger operator -(ModuloInteger val, int sub) => val + -sub;

                public static ModuloInteger operator ++(ModuloInteger val) => val + 1;
            }
        }
    }
}