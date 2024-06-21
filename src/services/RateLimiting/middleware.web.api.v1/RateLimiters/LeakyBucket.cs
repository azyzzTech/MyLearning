using System.Collections.Concurrent;
using System.Threading;

namespace middleware.web.api.v1.RateLimiters
{
    /// <summary>
    /// Implements the Leaky Bucket algorithm for rate limiting.
    /// 
    /// The Leaky Bucket algorithm is a rate limiting technique where incoming requests
    /// are treated like water drops added to a bucket. The bucket has a maximum capacity
    /// and a leak rate. Requests are accepted if the bucket has available capacity. The
    /// leak rate determines how quickly the bucket empties over time, ensuring that the
    /// system can maintain a steady rate of processing requests.
    /// 
    /// Key components:
    /// - Bucket Capacity: Maximum number of requests the bucket can hold at any time.
    /// - Leak Rate: Rate at which the bucket leaks or drains over time.
    /// - Semaphore: Ensures thread safety when accessing the bucket to prevent race conditions.
    /// </summary>
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
