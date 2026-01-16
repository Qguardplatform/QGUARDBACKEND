using qguardbackend.Core.Exceptions;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DTOs.Results;
using System.Net;
using System.Text.Json;

namespace qguardbackend.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            HttpStatusCode statusCode = HttpStatusCode.InternalServerError;
            string responseContent;

            switch (exception)
            {
                case ValidationException validationException:
                    statusCode = HttpStatusCode.BadRequest;
                    var result = CustomResult<string>.ValidationFailure(((int)statusCode).ToString(), validationException.ValidationErrors);
                    result.Message = string.Join(", ", validationException.ValidationErrors);
                    responseContent = JsonSerializer.Serialize(result);
                    break;

                case BadRequestException:
                case CustomException:
                case AccountLockedException:
                    statusCode = HttpStatusCode.BadRequest;
                    responseContent = JsonSerializer.Serialize(CustomResult<string>.ErrorOccured(exception.Message, ((int)statusCode).ToString()));
                    break;

                case NotFoundException:
                    statusCode = HttpStatusCode.NotFound;
                    responseContent = JsonSerializer.Serialize(CustomResult<string>.ErrorOccured(exception.Message, ((int)statusCode).ToString()));
                    break;

                default:
                    statusCode = HttpStatusCode.InternalServerError;
                    responseContent = JsonSerializer.Serialize(CustomResult<string>.Failure(CustomError.SystemExceptionError, ResponseCodes.SystemExceptionErrorCode));
                    break;
            }
            context.Response.StatusCode = (int)statusCode;

            _logger.LogError(exception, "Exception {ExceptionType} occurred at {Path} with message: {Message}",
                        exception.GetType().Name,
                        context.Request.Path,
                        exception.Message);
            await context.Response.WriteAsync(responseContent);
        }
    }
}
