using qguardbackend.Api.ServiceExtensions;
using qguardbackend.Data.DTOs;

//using qguardbackend.ServiceExtensions;
using Serilog;
using System.Text.Json.Serialization;

try
{
    var builder = WebApplication.CreateBuilder(args);

    ApplicationServicesExtension.AddSerilogWithMSSQL(builder);

    builder.Services.AddControllers()
        .AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });

    builder.Services.ConfigureServices(builder.Configuration);
    // ? Register HttpClientFactory
    builder.Services.AddHttpClient();
    builder.Services.AddApplicationOptions();
    //builder.Services.AddTransient<CustomHttpHandler>();
    //builder.Services.AddHttpClient("clientRequest").AddHttpMessageHandler<CustomHttpHandler>();

    var app = builder.Build();

    app.ConfigurePipeline();

    Log.Information("Exam portal API is starting at {@date}", DateTime.Now);
    app.Run();
    Log.Information("Exam portal API started at {@date}", DateTime.Now);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Exam portal API failed to start.");
}
finally
{
    Log.CloseAndFlush();
};




//-------------------------

public static class ApplicationOptionsExtension
{
    public static IServiceCollection AddApplicationOptions(this IServiceCollection services)
    {
        //services
        //.AddOptions<SendGridOptions>()
        //.BindConfiguration("SendGridOptions")
        //.ValidateDataAnnotations()
        //.ValidateOnStart();

        //services
        //.AddOptions<NotificationOptions>()
        //.BindConfiguration("NotificationOptions")
        //.ValidateDataAnnotations()
        //.ValidateOnStart();

        //services
        //.AddOptions<PushNotificationOptions>()
        //.BindConfiguration("PushNotificationOptions")
        //.ValidateDataAnnotations()
        //.ValidateOnStart();

        //services
        //.AddOptions<OrderOptions>()
        //.BindConfiguration("OrderOptions")
        //.ValidateDataAnnotations()
        //.ValidateOnStart();

        //services
        //.AddOptions<MiddlewareServiceOptions>()
        //.BindConfiguration("MiddlewareServiceOptions")
        //.ValidateDataAnnotations()
        //.ValidateOnStart();

        //services
        //.AddOptions<SlidingWindowRateLimitingOptions>()
        //.BindConfiguration("SlidingWindowRateLimitingOptions")
        //.ValidateDataAnnotations()
        //.ValidateOnStart();

        services
        .AddOptions<AWS>()
        .BindConfiguration("AWS")
        .ValidateDataAnnotations()
        .ValidateOnStart();

        return services;
    }
}