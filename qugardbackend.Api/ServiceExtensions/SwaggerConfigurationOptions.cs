using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using Asp.Versioning.ApiExplorer;

namespace qguardbackend.Api.ServiceExtensions;

public class SwaggerConfigurationOptions(IApiVersionDescriptionProvider _provider) : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, CreateVersionInfo(description));
        }
    }

    private OpenApiInfo CreateVersionInfo(ApiVersionDescription description)
    {
        var info = new OpenApiInfo()
        {
            Title = "Examination Portal API",
            Version = description.ApiVersion.ToString(),
            Contact = new OpenApiContact
            {
                Email = "daniel.ogwu@vigilearn.com | ayeni.timileyin@vigilearn.com",
                Name = "daniel.ogwu@vigilearn.com | ayeni.timileyin@vigilearn.com"
            },
            Description = "API to power up the Examination Portal",

        };


        return info;
    }
}