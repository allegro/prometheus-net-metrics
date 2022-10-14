using Allegro.Prometheus.TrueRpsMetric;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

var app = builder.Build();
app.UseRouting();
app.UseMiddleware<TrueRpsMiddleware>(); // here the "true RPS metric" is being added
app.UseHttpMetrics();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapMetrics();
});
app.Run();