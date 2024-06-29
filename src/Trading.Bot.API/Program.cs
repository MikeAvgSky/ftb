
var builder = WebApplication.CreateBuilder(args);

// Configure Services

var constants = builder.Configuration
    .GetSection(nameof(Constants))
    .Get<Constants>();

builder.Services.AddSingleton(constants!);

builder.Services.AddOandaApiService(constants);

builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = int.MaxValue;
});

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = int.MaxValue;
});

builder.Services.Configure<FormOptions>(x =>
{
    x.ValueLengthLimit = int.MaxValue;
    x.MultipartBodyLengthLimit = int.MaxValue;
    x.MultipartHeadersLengthLimit = int.MaxValue;
});

builder.Services.AddMediatR(c =>
{
    c.Lifetime = ServiceLifetime.Scoped;

    c.RegisterServicesFromAssemblyContaining<Program>();
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure

app.UseSwagger();

app.UseSwaggerUI();

app.MapAccountEndpoints();

app.MapInstrumentEndpoints();

app.MapCandleEndpoints();

app.MapSimulationEndpoints();

app.Run();