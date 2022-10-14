using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Allegro.Prometheus.TrueRpsMetric;

/// <summary>
/// Middleware that collects the max RPS per second ("true RPS") per endpoint
/// over last minute (prometheus collection interval).
/// </summary>
public class TrueRpsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly MaxPerSecondGauge? _trueRpsMetric;

    public TrueRpsMiddleware(
        RequestDelegate next,
        IOptions<TrueRpsMetricConfiguration> configuration)
    {
        _next = next;
        _trueRpsMetric = TrueRpsMetricFactory.Create(configuration);
    }

    /// <summary>
    /// Middleware invocation
    /// </summary>
    public async Task Invoke(HttpContext context)
    {
        if (_trueRpsMetric == null)
        {
            await _next(context);
            return;
        }

        var actionName = context.GetRouteValue("action")?.ToString();
        var controllerName = context.GetRouteValue("controller")?.ToString();

        if (!string.IsNullOrEmpty(actionName) && !string.IsNullOrEmpty(controllerName))
        {
            _trueRpsMetric
                .WithLabels(controllerName, actionName, context.Request.Method)
                .Observe();
        }

        await _next(context);
    }
}