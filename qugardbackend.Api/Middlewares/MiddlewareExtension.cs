using examportal.Middlewares;

namespace examportal.Api.Middleware;

public static class MiddlewareExtension
{
    public static IApplicationBuilder UseCustomExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionMiddleware>();
    }
    public static IApplicationBuilder UserTenantAccessMiddlewareHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<UserTenantAccessMiddleware>();
    }
    public static IApplicationBuilder TenantvalidityMiddlewareHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantValidityMiddleware>();
    }

    public static IApplicationBuilder RequestResponseLoggingMiddlewarehandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestResponseLoggingMiddleware>();
    }
}