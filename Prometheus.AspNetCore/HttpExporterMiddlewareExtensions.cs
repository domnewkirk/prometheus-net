using Microsoft.AspNetCore.Builder;
using Prometheus.AspNetCore.HttpExporter;
using System;

namespace Prometheus.AspNetCore
{
    public static class HttpExporterMiddlewareExtensions
    {
        public static IApplicationBuilder UseHttpExporter(this IApplicationBuilder app,
            Action<HttpMiddlewareExporterOptions> configure)
        {
            var options = new HttpMiddlewareExporterOptions();

            configure?.Invoke(options);

            app.UseHttpExporter(options);

            return app;
        }

        public static IApplicationBuilder UseHttpExporter(this IApplicationBuilder app,
            HttpMiddlewareExporterOptions options = null)
        {
            if (options == null) options = new HttpMiddlewareExporterOptions();

            if (options.InFlight.Enabled)
                app.UseMiddleware<HttpInFlightMiddleware>(options.InFlight.Gauge);
            if (options.RequestCount.Enabled)
                app.UseMiddleware<HttpRequestCountMiddleware>(options.RequestCount.Counter);
            if (options.RequestDuration.Enabled)
                app.UseMiddleware<HttpRequestDurationMiddleware>(options.RequestDuration.Histogram);

            return app;
        }
    }
}