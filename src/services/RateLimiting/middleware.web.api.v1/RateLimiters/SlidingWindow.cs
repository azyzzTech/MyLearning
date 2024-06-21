using System.Collections.Concurrent;

namespace middleware.web.api.v1.RateLimiters
{
    /// <summary>
    /// Implements the Sliding Window algorithm for rate limiting.
    /// 
    /// The Sliding Window algorithm is a rate limiting technique that divides time into
    /// fixed intervals (windows) and counts the number of requests made within each window.
    /// Requests are accepted if the count does not exceed a specified threshold within 
    /// the current window. The window slides over time, allowing for a dynamic control 
    /// of request rates.
    /// 
    /// Key components:
    /// - Window Size: The duration of each sliding window interval.
    /// - Max Requests: The maximum number of requests allowed per sliding window interval.
    /// - ClientData: Stores request timestamps and counts per client for rate limiting.
    /// </summary>
    public class SlidingWindow
    {
        private readonly RequestDelegate _delegate;
        private readonly int _maxRequests;
        private readonly TimeSpan _windowSize;
        private static ConcurrentDictionary<string, ClientData> _clients = new ConcurrentDictionary<string, ClientData>();

        public SlidingWindow(RequestDelegate @delegate, int maxRequests, TimeSpan windowSize)
        {
            _delegate = @delegate;
            _maxRequests = maxRequests;
            _windowSize = windowSize;
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
            var clientData = _clients.GetOrAdd(clientId, _ => new ClientData());

            await clientData.Semaphore.WaitAsync();

            try
            {
                while (clientData.Requests.TryPeek(out var time) && now - time > _windowSize)
                {
                    clientData.Requests.TryDequeue(out _);
                }

                if (clientData.Requests.Count >= _maxRequests)
                {
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync("Too many requests. Please try again later.");
                    return;
                }

                clientData.Requests.Enqueue(now);
                clientData.LastRequest = now;
            }
            finally
            {
                clientData.Semaphore.Release();
            }

            await _delegate(context);
        }

        private class ClientData
        {
            public ConcurrentQueue<DateTime> Requests { get; } = new ConcurrentQueue<DateTime>();
            public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1, 1);
            public DateTime LastRequest { get; set; }

            public ClientData()
            {
                LastRequest = DateTime.UtcNow;
            }
        }
    }
}