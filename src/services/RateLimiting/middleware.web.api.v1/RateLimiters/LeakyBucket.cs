using System.Collections.Concurrent;
using System.Threading;

namespace middleware.web.api.v1.RateLimiters
{
    public class LeakyBucket
    {
        private readonly RequestDelegate _delegate;
        private readonly int _bucketCapacity;
        private readonly TimeSpan _leakRate;
        private static ConcurrentDictionary<string, BucketLeaky> _buckets = new ConcurrentDictionary<string, BucketLeaky>();

        public LeakyBucket(RequestDelegate @delegate, int bucketCapacity, TimeSpan leakRate)
        {
            _delegate = @delegate;
            _bucketCapacity = bucketCapacity;
            _leakRate = leakRate;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientId = context.Connection.RemoteIpAddress?.ToString();
            if (clientId == null)
            {
                await _delegate(context);
                return;
            }

            var bucket = _buckets.GetOrAdd(clientId, _ => new BucketLeaky(_bucketCapacity, _leakRate));

            await bucket.Semaphore.WaitAsync();

            try
            {
                if (bucket.TryConsume())
                {
                    await _delegate(context);
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync("Too many requests. Please try again later.");
                }
            }
            finally
            {
                bucket.Semaphore.Release();
            }
        }
    }

    public class BucketLeaky
    {
        private readonly int _capacity;
        private readonly TimeSpan _leakRate;
        private DateTime _lastLeakTime;
        private int _waterLevel;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public BucketLeaky(int capacity, TimeSpan leakRate)
        {
            _capacity = capacity;
            _leakRate = leakRate;
            _lastLeakTime = DateTime.UtcNow;
            _waterLevel = 0;
        }

        public SemaphoreSlim Semaphore => _semaphore;

        public bool TryConsume()
        {
            lock (this)
            {
                Leak();
                if (_waterLevel < _capacity)
                {
                    _waterLevel++;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private void Leak()
        {
            var now = DateTime.UtcNow;
            var timePassed = now - _lastLeakTime;
            var leakAmount = (int)(timePassed.TotalMilliseconds / _leakRate.TotalMilliseconds);
            _waterLevel = Math.Max(0, _waterLevel - leakAmount);
            _lastLeakTime = now;
        }
    }
}
