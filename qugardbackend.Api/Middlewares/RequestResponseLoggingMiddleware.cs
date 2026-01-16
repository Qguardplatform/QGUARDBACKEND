namespace qguardbackend.Middlewares
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Log Request
            var request = context.Request;
            request.EnableBuffering(); // Enable reading the body multiple times

            var requestBody = await new StreamReader(request.Body).ReadToEndAsync();
            request.Body.Position = 0;

            _logger.LogInformation("Incoming Request: {@Method} {@Path} {@Headers} {@Body}",
                request.Method,
                request.Path,
                request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                requestBody);

            // Log Response
            var originalBodyStream = context.Response.Body;

            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                await _next(context); // Continue down the pipeline

                context.Response.Body.Seek(0, SeekOrigin.Begin);
                var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
                context.Response.Body.Seek(0, SeekOrigin.Begin);

                _logger.LogInformation("Response: {@StatusCode} {@Body}",
                    context.Response.StatusCode,
                    responseText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while processing request");
                throw; // Re-throw to be handled elsewhere
            }
            finally
            {
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
    }

}
