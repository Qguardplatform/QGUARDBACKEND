using Asp.Versioning;

namespace examportal.Api.ServiceExtensions;

public static class ApiVersioningExtension
{
    public static IServiceCollection AddAPIVersioning(this IServiceCollection services)
    {
        var apiVersioningBuilder = services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.ReportApiVersions = true;
            options.AssumeDefaultVersionWhenUnspecified = true;
            //options.ApiVersionReader = new UrlSegmentApiVersionReader();
        });

        apiVersioningBuilder.AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }
}