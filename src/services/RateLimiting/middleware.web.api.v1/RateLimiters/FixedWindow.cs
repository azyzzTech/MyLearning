using System.Collections.Concurrent;

namespace middleware.web.api.v1.RateLimiters
{
    public class FixedWindow
    {
        private readonly RequestDelegate _delegate;
        private readonly TimeSpan _windowSize;
        private readonly int _maxRequests;
        private static ConcurrentDictionary<string, (int Count, DateTime WindowStart)> _clients = new ConcurrentDictionary<string, (int, DateTime)>();

        public FixedWindow(RequestDelegate @delegate, TimeSpan @windowSize, int @maxRequests)
        {
            _delegate = @delegate;
            _windowSize = @windowSize;
            _maxRequests = @maxRequests;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString();

            if (clientIp is null)
            {
                await _delegate(context);
                return;
            }

            var now = DateTime.UtcNow;
            var clientData = _clients.GetOrAdd(clientIp, _ => (0, now));

            if (now > clientData.WindowStart + _windowSize)
            {
                clientData = (0, now);
            }

            if (clientData.Count >= _maxRequests)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("Too many requests. Please try again later.");
                return;
            }

            _clients[clientIp] = (clientData.Count + 1, clientData.WindowStart);

            await _delegate(context);
        }
    }
}
