using System.Text;
using examportal.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using edutech.services.examportal.Data.DbContext;

namespace examportal.Api.ServiceExtensions;

public static class AddAuthenticationExtension
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration _config, string authUrl, string audience)
    {
        var jwtSettings = _config.GetSection("JWT");

        services.AddIdentity<ApplicationUser, ApplicationRole>(opt =>
        {
            opt.Password.RequiredLength = 7;
            opt.Password.RequireDigit = false;
            opt.Password.RequireUppercase = false;
            opt.User.RequireUniqueEmail = true;
        })
         .AddEntityFrameworkStores<AppDbContext>()
         .AddDefaultTokenProviders();
        
        services.AddAuthentication(opt =>
        {
            opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            opt.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            var key = Encoding.UTF8.GetBytes(jwtSettings?.GetSection("Secret").Value);
            var validAudiences = jwtSettings.GetSection("Audiences").Get<string[]>();
            var validIssuers = jwtSettings.GetSection("Issuers").Get<string[]>();

            //options.Authority = $"https://securetoken.google.com/{builder.Configuration["Jwt:ValidAudience"]}";
            options.Authority = _config["Jwt:ValidIssuer"];

            options.SaveToken = true;
            options.IncludeErrorDetails = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = false,
                //ValidAudiences = validAudiences,
                //ValidIssuers = validIssuers,

                ValidIssuer = _config["Jwt:ValidIssuer"],
                ValidAudience = _config["Jwt:ValidAudience"],

                IssuerSigningKey = new SymmetricSecurityKey(key)
            };
        });

        return services;
    }
}