using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace qguardbackend.Api.ServiceExtensions;

public static class SwaggerConfigurationExtension
{
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.ConfigureOptions<SwaggerConfigurationOptions>();
        services.AddSwaggerGen(options =>
        {         
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            options.IncludeXmlComments(xmlPath);

            // add JWT Authentication
            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "JWT Authentication",
                Description = "Enter JWT Bearer token **_only_**",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer", // must be lower case
                BearerFormat = "JWT",
                Reference = new OpenApiReference
                {
                    Id = JwtBearerDefaults.AuthenticationScheme,
                    Type = ReferenceType.SecurityScheme
                }
            };
            options.OperationFilter<FileUploadOperationFilter>();
            options.OperationFilter<SecurityRequirementsOperationFilter>();
            options.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    securityScheme, new string[] { }
                }
            });
        });

        return services;
    }
}

internal class SecurityRequirementsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        //operation.Parameters.Add(new OpenApiParameter
        //{
        //    Name = "TenantCode",
        //    Description = "The TenantCode of the Client Host calling the API",
        //    In = ParameterLocation.Header,
        //    Schema = new OpenApiSchema() { Type = "string" },
        //    Required = true

        //});

        var anonControllerScope = context
            .MethodInfo
            .DeclaringType
            .GetCustomAttributes(true)
            .OfType<AllowAnonymousAttribute>();

        var anonMethodScope = context
                .MethodInfo
                .GetCustomAttributes(true)
                .OfType<AllowAnonymousAttribute>();

        // only add authorization specification information if there is at least one 'Authorize' in the chain and NO method-level 'AllowAnonymous'
        if (!anonMethodScope.Any() && !anonControllerScope.Any())
        {
            // add generic message if the controller methods dont already specify the response type
            if (!operation.Responses.ContainsKey("401"))
                operation.Responses.Add("401", new OpenApiResponse { Description = "If Authorization header not present, has no value or no valid jwt bearer token" });

            if (!operation.Responses.ContainsKey("403"))
                operation.Responses.Add("403", new OpenApiResponse { Description = "If user not authorized to perform requested action" });

            var jwtAuthScheme = new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            };

            operation.Security = new List<OpenApiSecurityRequirement>
            {
                new OpenApiSecurityRequirement
                {
                    [ jwtAuthScheme ] = new List<string>()
                }
            };
        }
    }
}

public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var fileParameters = context.MethodInfo.GetParameters()
            .Where(p => p.ParameterType == typeof(IFormFile) || p.ParameterType == typeof(IEnumerable<IFormFile>));

        foreach (var parameter in fileParameters)
        {
            operation.Parameters.Remove(operation.Parameters
                .FirstOrDefault(p => p.Name == parameter.Name));

            operation.RequestBody = new OpenApiRequestBody
            {
                Content =
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties =
                            {
                                [parameter.Name] = new OpenApiSchema
                                {
                                    Type = "string",
                                    Format = "binary"
                                }
                            },
                            Required = new HashSet<string> { parameter.Name }
                        }
                    }
                }
            };
        }
    }
}