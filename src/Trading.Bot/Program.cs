
var builder = WebApplication.CreateBuilder(args);

// Configure Services

var constants = builder.Configuration.GetSection(nameof(Constants)).Get<Constants>();

builder.Services.AddSingleton(constants);

var tradeConfiguration = builder.Configuration.GetSection(nameof(TradeConfiguration)).Get<TradeConfiguration>();

builder.Services.AddSingleton(tradeConfiguration);

builder.Services.AddOandaApiService(constants);

builder.Services.AddOandaStreamService(constants);

builder.Services.AddSerilogLogging(builder.Configuration);

builder.Services.AddMediatR(c =>
{
    c.Lifetime = ServiceLifetime.Scoped;

    c.RegisterServicesFromAssemblyContaining<Program>();
});

if (constants.RunBot)
{
    builder.Services.AddHostedService<StreamProcessor>();

    builder.Services.AddHostedService<TradeManager>();
}

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
