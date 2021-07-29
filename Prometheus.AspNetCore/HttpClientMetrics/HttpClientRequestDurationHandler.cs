using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Prometheus.HttpClientMetrics
{
    internal sealed class HttpClientRequestDurationHandler : HttpClientDelegatingHandlerBase<ICollector<IHistogram>, IHistogram>
    {
        public HttpClientRequestDurationHandler(HttpClientRequestDurationOptions? options)
            : base(options, options?.Histogram)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var stopWatch = ValueStopwatch.StartNew();

            try
            {
                // We measure until SendAsync returns - which is when the response HEADERS are seen.
                // The response body may continue streaming for a long time afterwards, which this does not measure.
                return await base.SendAsync(request, cancellationToken);
            }
            finally
            {
                CreateChild(request).Observe(stopWatch.GetElapsedTime().TotalSeconds);
            }
        }

        protected override ICollector<IHistogram> CreateMetricInstance(string[] labelNames) => MetricFactory.CreateHistogram(
            "httpclient_request_duration_seconds",
            "Duration histogram of HTTP requests performed by an HttpClient.",
            new HistogramConfiguration
            {
                // 1 ms to 32K ms buckets
                Buckets = Histogram.ExponentialBuckets(0.001, 2, 16),
                LabelNames = labelNames
            });
    }
}