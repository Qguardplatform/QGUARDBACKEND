using qguardbackend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using qguardbackend.Data.DbContext;
using Amazon.S3;
using qguardbackend.Core.Interfaces;
using qguardbackend.Core.Services;
using qguardbackend.Core.Profiles;
using Asp.Versioning.ApiExplorer;
using qguardbackend.Api.Middleware;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using qguardbackend.Core.ApplicationOptions;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using System.Collections.ObjectModel;
using qguardbackend.Core.ConfigModels;
using qguardbackend.BoilerPlate.Service.Interfaces;
using qguardbackend.BoilerPlate.Service.Implementations;
using qguardbackend.Core.BackGroundService;
using Amazon.Runtime.Internal.Util;

namespace qguardbackend.Api.ServiceExtensions;

public static class ApplicationServicesExtension
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConString"),
                            b => b.MigrationsAssembly("qguardbackend.Data"))
                       .EnableSensitiveDataLogging()
        );

        // Log using Serilog's static logger
        Log.Information("Database Connection String: {ConnectionString}", configuration.GetConnectionString("DefaultConString"));


        //Automapper
        services.AddAutoMapper(typeof(AutoMapperProfile));

        services.AddAWSService<IAmazonS3>();
        services.AddSingleton<EmailNotificationChannel>();
        //services.AddHostedService<EmailNotificationBackgroundService>();

        services.AddSingleton<IBackgroundEmailQueue, BackgroundEmailQueue>();
        services.AddHostedService<CandidateUploadEmailWorker>();

        //configure services
        services.AddScoped<IS3Service, S3Service>();
        //services.AddScoped<IInstitutionService, InstitutionService>();
        services.AddScoped<IAuthService, AuthService>();
        //services.AddScoped<IHttpService, HttpService>();
        services.AddScoped<IEmailService, EmailService>();
        //services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IAzureService, AzureService>();
        //services.AddScoped<ICandidateService, CandidateService>();
        //services.AddScoped<IDepartmentService, DepartmentService>();
        //services.AddScoped<IFacultyService, FacultyService>();
        //services.AddScoped<ILevelService, LevelService>();
        //services.AddScoped<IProgramService, ProgramService>();
        //services.AddScoped<IQuestionBankService, QuestionBankService>();
        services.AddScoped<IRoleService, RoleService>();
        //services.AddScoped<ISemesterService, SemesterService>();
        //services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IStaticDatas, StaticDatas>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        //services.AddScoped<IExamScheduleService, ExamScheduleService>();
        //services.AddScoped<IExamService, ExamService>();
        //services.AddScoped<ICourseService, CourseService>();
        //services.AddScoped<ITutorsService, TutorsService>();
        //services.AddScoped<IProctorService, ProctorService>();
        //services.AddScoped<IDashboardService, DashboardService>();
        //services.AddScoped<IEnforcementActionService, EnforcementActionService>();
        //services.AddScoped<IEnforcementModeService, EnforcementModeService>();
        //services.AddScoped<IProctorMeTrackerService, ProctorMeTrackerService>();

        return services;
    }

    public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 6;
            options.User.RequireUniqueEmail = false;
            options.SignIn.RequireConfirmedAccount = false;
            options.SignIn.RequireConfirmedEmail = false;

        }).AddEntityFrameworkStores<AppDbContext>()
          .AddDefaultTokenProviders();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            var jwtSection = configuration.GetSection("JWT");
            var audiences = jwtSection.GetSection("Audiences").Get<string[]>();
            var issuers = jwtSection.GetSection("Issuers").Get<string[]>();

            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Secret"])),
                ValidIssuers = issuers,
                ValidAudiences = audiences
            };
        });

        services.AddAuthorization();

        return services;
    }

    public static void Seeder(this IApplicationBuilder app)
    {
        using (var scope = app.ApplicationServices.CreateScope())
        {
            var services = scope.ServiceProvider;

            try
            {
                var context = services.GetRequiredService<AppDbContext>();
                var settingSvc = services.GetRequiredService<ISettingsService>();
                var staticSvc = services.GetRequiredService<IStaticDatas>();
                var roleSvc = services.GetRequiredService<IRoleService>();
                var uSvc = services.GetRequiredService<IUserManagementService>();
                //var iISvc = services.GetRequiredService<IInstitutionService>();
                //var semSvc = services.GetRequiredService<ISemesterService>();
                //var sesSvc = services.GetRequiredService<ISessionService>();
                //var aSvc = services.GetRequiredService<IEnforcementActionService>();
                //var mSvc = services.GetRequiredService<IEnforcementModeService>();

                DbContextInitializer.Initialize(context, settingSvc, staticSvc, roleSvc, uSvc/*, iISvc, semSvc, sesSvc, aSvc, mSvc*/).Wait();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message} \n {ex.InnerException} \n {ex.StackTrace}");
            }
            finally
            {
                // Reset IsSeeding flag after seeding is complete
                var context = services.GetRequiredService<AppDbContext>();
            }
        }
    }

    public static void UpdateDatabase(this IApplicationBuilder app)
    {
        using (var serviceScope = app.ApplicationServices.CreateScope())
        {
            var dbContext = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Database.Migrate();
        }
    }

    public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();

        // Add services to the container.
        services.AddSwaggerConfiguration()
                .AddAPIVersioning()
                .AddApplicationServices(configuration)
                .AddIdentityServices(configuration)
                .AddHttpContextAccessor();

        services.AddControllers().AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
            options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        });

        //configure settings
        services.Configure<AzureSetting>(configuration.GetSection("AzureSetting"));
        services.Configure<MailSetting>(configuration.GetSection("MailSetting"));
    }

    public static void AddSerilogWithMSSQL(this WebApplicationBuilder builder)
    {
        var config = builder.Configuration;
        var connectionString = config.GetConnectionString("DefaultConString");

        var columnOptions = new ColumnOptions
        {
            Store = new Collection<StandardColumn>
            {
                StandardColumn.Message,
                StandardColumn.MessageTemplate,
                StandardColumn.Level,
                StandardColumn.TimeStamp,
                StandardColumn.Exception,
                StandardColumn.Properties
            }
        };

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day)
            .WriteTo.MSSqlServer(
                connectionString: connectionString!,
                sinkOptions: new MSSqlServerSinkOptions
                {
                    TableName = "SystemLogs",
                    AutoCreateSqlTable = true
                },
                columnOptions: columnOptions,
                restrictedToMinimumLevel: LogEventLevel.Information)
            .CreateLogger();

        builder.Host.UseSerilog();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging();
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            using var scope = app.Services.CreateScope();
            var provider = scope.ServiceProvider.GetRequiredService<IApiVersionDescriptionProvider>();

            foreach (var description in provider.ApiVersionDescriptions)
            {
                // Display name: "API v1", "API v2", etc.
                var groupName = description.GroupName.ToUpperInvariant();
                options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", $"API {groupName}");
            }
        });

        app.UseCors(
            options => options.SetIsOriginAllowed(x => _ = true).AllowAnyMethod().AllowAnyHeader().AllowCredentials()
        );
        app.Seeder();

        app.UseNWebSecurity();

        //app.TenantvalidityMiddlewareHandler();

        //app.UserTenantAccessMiddlewareHandler();

        app.UseStaticFiles();

        app.UseHttpsRedirection();

        app.UseAuthentication();

        app.UseAuthorization();

        app.MapControllers();

        app.UpdateDatabase();

        app.UseCustomExceptionHandler();
        app.RequestResponseLoggingMiddlewarehandler();

        return app;
    }
}