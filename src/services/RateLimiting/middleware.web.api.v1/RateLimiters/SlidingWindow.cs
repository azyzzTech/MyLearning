using System.Collections.Concurrent;

namespace middleware.web.api.v1.RateLimiters
{
    public class SlidingWindow
    {
        private readonly RequestDelegate _next;
        private readonly int _maxRequests;
        private readonly TimeSpan _windowSize;
        private static ConcurrentDictionary<string, ClientData> _clients = new ConcurrentDictionary<string, ClientData>();

        public SlidingWindow(RequestDelegate next, int maxRequests, TimeSpan windowSize)
        {
            _next = next;
            _maxRequests = maxRequests;
            _windowSize = windowSize;
        }

        /// <summary>
        /// Middleware that applies sliding window rate limiting based on client IP address.
        /// Limits the number of requests a client can make within a specified time window.
        /// </summary>
        /// <param name="context">The HttpContext of the current request.</param>
        /// <returns>A Task that represents the completion of request processing.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            var clientId = context.Connection.RemoteIpAddress?.ToString();

            if (clientId == null)
            {
                await _next(context);
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

            await _next(context);
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