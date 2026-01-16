using qguardbackend.Api.ServiceExtensions;
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