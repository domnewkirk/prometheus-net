﻿using Prometheus;
using Prometheus.Advanced;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace tester
{
    internal class Program
    {
        private static void Main()
        {
            // Replace the first line with an appropriate type of tester to run different manual tests.
            var tester = new MetricPusherTester();

            // For testing Kestrel metric server with HTTPS, you need at least a self-signed certificate (one included here)
            // and the matching domain pointed to 127.0.0.1 (e.g. hardcoded in the PCs hosts file) and you also need to
            // import this certificate into your Trusted Root Certification Authorities certificate store to trust it.
            //var certificate = new X509Certificate2("prometheus-net.test.pfx", "prometheus-net.test");
            //var tester = new KestrelMetricServerTester("prometheus-net.test", certificate);

            tester.OnStart();

            var metricServer = tester.InitializeMetricServer();
            metricServer?.Start();

            var counter = Metrics.CreateCounter("myCounter", "help text", new CounterConfiguration
            {
                LabelNames = new[] { "method", "endpoint" }
            });
            counter.WithLabels("GET", "/").Inc();
            counter.WithLabels("POST", "/cancel").Inc();

            Metrics.CreateCounter("always_zero", "This counter is always zero but still needs to be present in the output!");

            var gauge = Metrics.CreateGauge("gauge", "help text");
            gauge.Inc(3.4);
            gauge.Dec(2.1);
            gauge.Set(5.3);

            // As the initial value is suppressed and a new one never assigned, this one never shows up in the export.
            Metrics.CreateGauge("should_not_show_up", "", new GaugeConfiguration
            {
                SuppressInitialValue = true
            });

            var hist = Metrics.CreateHistogram("myHistogram", "help text", new HistogramConfiguration
            {
                Buckets = new[] { 0, 0.2, 0.4, 0.6, 0.8, 0.9 }
            });
            hist.Observe(0.4);

            var timedHistogram = Metrics.CreateHistogram("myTimedHistogram", "help text", new HistogramConfiguration
            {
                Buckets = new[] { 0, 0.2, 0.4, 0.6, 0.8, 0.9 }
            });

            var latestGauge = Metrics.CreateGauge("latestGauge", "Reports the latest cycle time");

            var summary = Metrics.CreateSummary("mySummary", "help text");
            summary.Observe(5.3);

            // Exmaple implementation of a custom collector.
            DefaultCollectorRegistry.Instance.GetOrAdd(new ExternalDataCollector());

            // Example implementation of on-demand collection.
            DefaultCollectorRegistry.Instance.RegisterOnDemandCollector<OnDemandCollection>();

            // Uncomment this to test deliberately causing collections to fail. This should result in 503 responses.
            // With MetricPusherTester you might get a 1st push already before it fails but after that it should stop pushing.
            //DefaultCollectorRegistry.Instance.RegisterOnDemandCollectors(new AlwaysFailingOnDemandCollector());

            var cts = new CancellationTokenSource();

            var random = new Random();

            // Update metrics on a regular interval until told to stop.
            var updateInterval = TimeSpan.FromSeconds(0.5);
            var updateTask = Task.Factory.StartNew(async delegate
            {
                while (!cts.IsCancellationRequested)
                {
                    using (latestGauge.NewTimer())
                    using (timedHistogram.NewTimer())
                    {
                        var duration = Stopwatch.StartNew();

                        counter.Inc();
                        counter.Labels("GET", "/").Inc(2);
                        gauge.Set(random.NextDouble() + 2);
                        hist.Observe(random.NextDouble());
                        summary.Observe(random.NextDouble());

                        try
                        {
                            tester.OnTimeToObserveMetrics();
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine(ex);
                        }

                        var sleepTime = updateInterval - duration.Elapsed;

                        if (sleepTime > TimeSpan.Zero)
                            await Task.Delay(sleepTime, cts.Token);
                    }
                }
            }).Result;

            Console.WriteLine("Press enter to stop metricServer");
            Console.ReadLine();

            cts.Cancel();
            try
            {
                updateTask.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
            }

            metricServer?.StopAsync().GetAwaiter().GetResult();

            tester.OnEnd();

            Console.WriteLine("Press enter to stop tester");
            Console.ReadLine();
        }
    }
}
