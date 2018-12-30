using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Prometheus;
using Prometheus.HttpExporter.AspNetCore.InFlight;

namespace Tests.HttpExporter.AspNetCore
{
    [TestClass]
    public class InFlightMiddlewareTests
    {
        [TestMethod]
        public void Given_no_requests_then_InFlightGauge_returns_zero()
        {
            Assert.AreEqual(0, _gauge.IncrementCount);
            Assert.AreEqual(0, _gauge.DecrementCount);
            Assert.AreEqual(0, _gauge.Value);
        }

        [TestMethod]
        public async Task
            Given_multiple_completed_parallel_requests_gauge_is_incremented_and_decremented_correct_number_of_times()
        {
            await Task.WhenAll(_sut.Invoke(new DefaultHttpContext()), _sut.Invoke(new DefaultHttpContext()),
                _sut.Invoke(new DefaultHttpContext()));

            Assert.AreEqual(3, _gauge.IncrementCount);
            Assert.AreEqual(3, _gauge.DecrementCount);
            Assert.AreEqual(0, _gauge.Value);
        }
       
        
        [TestMethod]
        public async Task Given_request_throws_then_InFlightGauge_is_decreased()
        {
            this._requestDelegate = context => throw new InvalidOperationException();
            _sut = new HttpInFlightMiddleware(_requestDelegate, _gauge);

            
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => _sut.Invoke(new DefaultHttpContext()));
            
            Assert.AreEqual(1, _gauge.IncrementCount);
            Assert.AreEqual(1, _gauge.DecrementCount);
            Assert.AreEqual(0, _gauge.Value);
        }

        [TestInitialize]
        public void Init()
        {
            this._gauge = new FakeGauge(); 
            this._requestDelegate = context => Task.CompletedTask;
            
            _sut = new HttpInFlightMiddleware(this._requestDelegate, this._gauge);
        }

        private HttpInFlightMiddleware _sut;
        private FakeGauge _gauge;
        private RequestDelegate _requestDelegate;
    }

    internal class FakeGauge : IGauge
    {
        public int IncrementCount { get; private set; } = 0;
        public int DecrementCount { get; private set; } = 0;
        
        
        public void Inc(double increment = 1)
        {
            IncrementCount++;
            this.Value += increment;
        }

        public void Set(double val)
        {
            this.Value = val;
        }

        public void Dec(double decrement = 1)
        {
            DecrementCount++;
            this.Value -= decrement;
        }

        public double Value { get; private set; }
    }
}