using SearchAPI.Application;
using SearchAPI.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(o =>
{
    // Shared.Model.BEDocument exposes public fields (mId, mUrl, ...), not properties.
    o.JsonSerializerOptions.IncludeFields = true;
});

// Onion composition root: API depends inward on Application, which depends on Core.
// Infrastructure supplies the adapters behind Core's ports and is wired in only here.
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

// Identifies which instance answered, so clients (and the load balancer demo)
// can see requests spread across replicas. Set per replica in docker-compose.
var instanceName = builder.Configuration["INSTANCE_NAME"] ?? Environment.MachineName;
app.Use((context, next) =>
{
    context.Response.Headers["X-Served-By"] = instanceName;
    return next(context);
});

app.MapControllers();

app.Run();
