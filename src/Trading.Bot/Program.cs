
var builder = WebApplication.CreateBuilder(args);

var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

// Configure Services

var constants = builder.Configuration
    .GetSection(nameof(Constants))
    .Get<Constants>();

builder.Services.AddSingleton(constants);

var tradeConfiguration = builder.Configuration.
    GetSection(nameof(TradeConfiguration))
    .Get<TradeConfiguration>();

builder.Services.AddSingleton(tradeConfiguration);

var emailConfig = builder.Configuration
    .GetSection("EmailConfiguration")
    .Get<EmailConfiguration>();

builder.Services.AddSingleton(emailConfig);

builder.Services.AddTransient<EmailService>();

builder.Services.AddOandaApiService(constants);

builder.Services.AddOandaStreamService(constants);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

if (tradeConfiguration.RunBot)
{
    builder.Services.AddSingleton<LiveTradeCache>();

    builder.Services.AddHostedService<TradeManager>();

    builder.Services.AddHostedService<StreamProcessor>();

    builder.Services.AddHostedService<StreamWorker>();

    if (tradeConfiguration.StopRollover)
    {
        builder.Services.AddHostedService<RolloverManager>();
    }
}

if (isDevelopment)
{
    builder.Services.AddMediatR(c =>
    {
        c.Lifetime = ServiceLifetime.Scoped;

        c.RegisterServicesFromAssemblyContaining<Program>();
    });

    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddSwaggerGen();
}

var app = builder.Build();

// Configure

if (isDevelopment)
{
    app.UseSwagger();

    app.UseSwaggerUI();

    app.MapAccountEndpoints();

    app.MapInstrumentEndpoints();

    app.MapCandleEndpoints();

    app.MapSimulationEndpoints();
}

app.Run();