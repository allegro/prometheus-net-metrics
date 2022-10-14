# Allegro.Prometheus.TrueRpsMetric

This library contains the "true RPS" aka "max RPS in last scrapping period" metric, based on prometheus-net library.

## What is the "true RPS" metric

The [prometheus-net](https://github.com/prometheus-net/prometheus-net) library provides some useful metrics for monitoring the HTTP traffic
of your app, such as `http_request_duration_seconds` histogram or the `http_requests_in_progress` gauge. However, when monitoring a high
throughput system it is sometimes hard to notice very short peaks in HTTP calls - let's say originating from some marketing campaign.
The typical scrapping interval for Prometheus is 1 minute, but even shorter intervals might be too long to catch a peak that lasted for
a few seconds.

This is where the idea of the "true RPS" metric came from. At Allegro Pay, we wanted to be aware that our services received short, but high
peaks of incoming calls. The "true RPS" metric can be also described as "max RPS over scrapping period". Every time prometheus scrapes
the metrics endpoint, the `http_request_max_rps` gives the max RPS that was observed since the previous scrape. 

To avoid the collected metrics to be disturbed by developers or administrators manually calling the metrics endpoint, there is 
a `MinPublishInterval` configuration property available. If the interval between two scrapes is shorter than the `MinPublishInterval`, 
the counter will not be reset. This should be set to an interval just a bit shorter than prometheus scrapping interval.

## How to...

### Use the metric

Just add the `TrueRpsMiddleware` somewhere between the `UseRouting()` and `UseEndpoints()` invocations.

```csharp
app.UseRouting();
(...)
app.UseMiddleware<TrueRpsMiddleware>();
(...)
app.UseHttpMetrics();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapMetrics();
});
```

### Use custom collector registry

```csharp
services.Configure<TrueRpsMetricConfiguration>(opt => opt.Registry = myRegistry);
```

### Run the Demo

You can test the metric using the provided Demo app. It is located under `/samples/Allegro.Prometheus.Demo`. Just run the project and call following endpoints:

```
GET https://localhost:7274/demo
GET https://localhost:7274/metrics 
```

You should be able to see the `http_request_max_rps` metric.