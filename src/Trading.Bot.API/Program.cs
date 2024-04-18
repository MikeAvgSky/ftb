
var builder = WebApplication.CreateBuilder(args);

// Configure Services

var constants = builder.Configuration
    .GetSection(nameof(Constants))
    .Get<Constants>();

builder.Services.AddOandaApiService(constants);

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