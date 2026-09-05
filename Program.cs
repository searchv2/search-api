using SearchAPI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(o =>
{
    // BEDocument exposes public fields (mId, mUrl, ...), not properties.
    o.JsonSerializerOptions.IncludeFields = true;
});

// Postgres isn't provisioned yet - MockDatabase stands in until it is. Once it exists,
// swap this one line for: builder.Services.AddSingleton<IDatabase>(new DatabasePostgres());
builder.Services.AddSingleton<IDatabase>(new MockDatabase());

var app = builder.Build();

app.MapControllers();

app.Run();
