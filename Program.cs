using Microsoft.FeatureManagement;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using SearchAPI.Application;
using SearchAPI.Infrastructure;
using SearchAPI.Infrastructure.Persistence;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Feature flags come from a ConfigMap-mounted file so a feature can be disabled without a new
// image or a restart; appsettings.json holds the defaults when the file is absent.
builder.Configuration.AddJsonFile("/config/featureflags.json", optional: true, reloadOnChange: true);
builder.Services.AddFeatureManagement();

builder.Services.AddControllers().AddJsonOptions(o =>
{
    // Shared.Model.BEDocument exposes public fields (mId, mUrl, ...), not properties.
    o.JsonSerializerOptions.IncludeFields = true;
});

// Onion composition root: API depends inward on Application, which depends on Core.
// Infrastructure supplies the adapters behind Core's ports and is wired in only here.
builder.Services.AddApplication();
builder.Services.AddInfrastructure();
var redisEndpoint = builder.Configuration["RedisConnectionString"] ?? "localhost:6379";
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.ConfigurationOptions = new ConfigurationOptions
    {
        EndPoints = { redisEndpoint },
        User = builder.Configuration["REDISUSER"],
        Password = builder.Configuration["REDISPASSWORD"],
        ConnectTimeout = 5000,
        SyncTimeout = 5000,
    };
    options.InstanceName = "SearchV2_";
});

// Export only when an OTLP endpoint is configured (the Kubernetes deployment sets it).
if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
{
    var resource = ResourceBuilder.CreateDefault()
        .AddService("search-api", serviceInstanceId: Environment.MachineName);
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(r => r.AddService("search-api", serviceInstanceId: Environment.MachineName))
        .WithMetrics(metrics => metrics
            .AddMeter("Microsoft.AspNetCore.Hosting", "System.Net.Http", "System.Runtime",
                PostgresDocumentIndex.MeterName, SearchService.Meter.Name, SearchAPI.Api.Controllers.SearchController.Meter.Name)
            .AddOtlpExporter());
    builder.Logging.AddOpenTelemetry(logging =>
    {
        logging.IncludeFormattedMessage = true;
        logging.SetResourceBuilder(resource).AddOtlpExporter();
    });
}

var app = builder.Build();

// Identifies the replica that answered; Kubernetes defaults to the pod hostname.
var instanceName = builder.Configuration["INSTANCE_NAME"] ?? Environment.MachineName;
app.Use((context, next) =>
{
    context.Response.Headers["X-Served-By"] = instanceName;
    return next(context);
});

app.MapControllers();

app.MapGet("/healthz", () => Results.Ok("Healthy"));

app.Run();
