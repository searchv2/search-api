using SearchAPI.Api.Filters;
using SearchAPI.Application;
using SearchAPI.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiExceptionFilter>();
    options.Filters.Add<ValidateSearchRequestFilter>();
}).AddJsonOptions(o =>
{
    // Shared.Model.BEDocument exposes public fields (mId, mUrl, ...), not properties.
    o.JsonSerializerOptions.IncludeFields = true;
});

// Onion composition root: API depends inward on Application, which depends on Core.
// Infrastructure supplies the adapters behind Core's ports and is wired in only here.
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

app.MapControllers();

app.Run();
