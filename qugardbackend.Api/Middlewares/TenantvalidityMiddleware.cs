using qguardbackend.Core.Interfaces;

namespace qguardbackend.Middlewares
{
    public class TenantValidityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public TenantValidityMiddleware(RequestDelegate next, IServiceScopeFactory serviceScopeFactory)
        {
            _next = next;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task Invoke(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower();

            // Skip tenant validation for certain endpoints
            if (path == "/api/v1/account/auth" || path == "/api/v1/institution/create")
            {
                await _next(context);
                return;
            }

            // Check for TenantCode header
            if (!context.Request.Headers.TryGetValue("TenantCode", out var tenantCode))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Tenant code is missing");
                return;
            }

            using var scope = _serviceScopeFactory.CreateScope();
            //var institutionService = scope.ServiceProvider.GetRequiredService<IInstitutionService>();

            //var hasPermission = await institutionService.CheckinstitutionByCode(tenantCode);

            //if (!hasPermission)
            //{
            //    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            //    await context.Response.WriteAsync("Invalid tenant code passed!");
            //    return;
            //}

            await _next(context);
        }
    }
}
