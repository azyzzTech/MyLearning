using System.Collections.Concurrent;
using System.Net.Sockets;

namespace middleware.web.api.v1.RateLimiters
{
    public class TokenBucket
    {
        private readonly RequestDelegate _delegate;
        private readonly int _bucketCapacity;
        private readonly TimeSpan _refillInterval;
        private static ConcurrentDictionary<string, Bucket> _buckets = new ConcurrentDictionary<string, Bucket>();

        public TokenBucket(RequestDelegate @delegate, int bucketCapacity, TimeSpan refillInterval)
        {
            _delegate = @delegate;
            _bucketCapacity = bucketCapacity;
            _refillInterval = refillInterval;
        }

        /// <summary>
        /// Middleware that applies token bucket rate limiting based on client IP address.
        /// Limits the number of requests a client can make within a specified interval.
        /// </summary>
        /// <param name="context">The HttpContext of the current request.</param>
        /// <returns>A Task that represents the completion of request processing.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            var clientId = context.Connection.RemoteIpAddress?.ToString();

            if (clientId == null)
            {
                await _delegate(context);
                return;
            }

            var now = DateTime.UtcNow;
            var bucket = _buckets.GetOrAdd(clientId, _ => new Bucket(_bucketCapacity, _refillInterval));

            await bucket.Semaphore.WaitAsync();

            try
            {
                bucket.Refill(now);

                if (bucket.Tokens > 0)
                {
                    bucket.Tokens--;
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

    public class Bucket
    {
        public int Tokens { get; set; }
        public DateTime LastRefill { get; private set; }
        public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1, 1);
        private readonly int _bucketCapacity;
        private readonly TimeSpan _refillInterval;

        public Bucket(int bucketCapacity, TimeSpan refillInterval)
        {
            _bucketCapacity = bucketCapacity;
            _refillInterval = refillInterval;
            Tokens = bucketCapacity;
            LastRefill = DateTime.UtcNow;
        }

        public void Refill(DateTime now)
        {
            var tokensToAdd = (int)((now - LastRefill).TotalMilliseconds / _refillInterval.TotalMilliseconds);

            if (tokensToAdd > 0)
            {
                Tokens = Math.Min(Tokens + tokensToAdd, _bucketCapacity);
                LastRefill = now;
            }
        }
    }
}