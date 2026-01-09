using qguardbackend.Core.Interfaces;
using qguardbackend.Data.DbContext;
using Microsoft.EntityFrameworkCore;

namespace examportal.Middlewares
{
    public class UserTenantAccessMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<UserTenantAccessMiddleware> _logger;

        public UserTenantAccessMiddleware(
            RequestDelegate next,
            ILogger<UserTenantAccessMiddleware> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _next = next;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                // Check if TenantCode is present
                if (!context.Request.Headers.TryGetValue("TenantCode", out var tenantCode))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync("Tenant code is missing");
                    return;
                }

                // AllowAnonymous: skip validation
                var endpoint = context.GetEndpoint();
                if (endpoint?.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>() != null)
                {
                    await _next(context);
                    return;
                }

                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var tokenService = scope.ServiceProvider.GetRequiredService<IUserManagementService>();
                var institutionService = scope.ServiceProvider.GetRequiredService<IInstitutionService>();

                var userClaims = await tokenService.GetUserClaim();
                var inst = await institutionService.GetByCode(tenantCode.ToString());

                if (inst?.Data == null)
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    await context.Response.WriteAsync("Invalid tenant code");
                    return;
                }

                var hasAccess = await dbContext.UserRoles
                    .AnyAsync(x => x.UserId == userClaims.UserId && x.InstitutionId == inst.Data.Id);

                var hasAccessAsSystemAdmin = await dbContext.SystemAdminOtherTenantsRole
                  .AnyAsync(x => x.UserId == userClaims.UserId && x.InstitutionId == inst.Data.Id);


                if (!hasAccess && !hasAccessAsSystemAdmin)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("User is not permitted to access this tenant, check the tenant code!");
                    return;
                }

                // Pass the request to the next middleware
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in UserTenantAccessMiddleware.");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("An error occurred while validating the user tenant access.");
            }
        }
    }
}