using System.Collections.Concurrent;
using System.Net.Sockets;

namespace middleware.web.api.v1.RateLimiters
{
    /// <summary>
    /// Implements the Token Bucket algorithm for rate limiting.
    /// 
    /// The Token Bucket algorithm is a rate limiting technique where tokens (representing
    /// requests) are added to a bucket at a specified rate. Each incoming request must 
    /// consume a token from the bucket. If the bucket is empty, further requests are 
    /// rejected until new tokens are added according to the refill rate.
    /// 
    /// Key components:
    /// - Bucket Capacity: Maximum number of tokens (requests) the bucket can hold at any time.
    /// - Refill Rate: Rate at which tokens (requests) are added to the bucket.
    /// - Semaphore: Ensures thread safety when accessing the bucket to prevent race conditions.
    /// </summary>
    public class TokenBucket
    {
        private readonly RequestDelegate _delegate;
        private readonly int _bucketCapacity;
        private readonly TimeSpan _refillInterval;
        private static ConcurrentDictionary<string, BucketToken> _buckets = new ConcurrentDictionary<string, BucketToken>();

        public TokenBucket(RequestDelegate @delegate, int bucketCapacity, TimeSpan refillInterval)
        {
            _delegate = @delegate;
            _bucketCapacity = bucketCapacity;
            _refillInterval = refillInterval;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientId = context.Connection.RemoteIpAddress?.ToString();

            if (clientId == null)
            {
                await _delegate(context);
                return;
            }

            var now = DateTime.UtcNow;
            var bucket = _buckets.GetOrAdd(clientId, _ => new BucketToken(_bucketCapacity, _refillInterval));

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

    public class BucketToken
    {
        public int Tokens { get; set; }
        public DateTime LastRefill { get; private set; }
        public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1, 1);
        private readonly int _bucketCapacity;
        private readonly TimeSpan _refillInterval;

        public BucketToken(int bucketCapacity, TimeSpan refillInterval)
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