
var builder = WebApplication.CreateBuilder(args);

// Configure Services

var constants = builder.Configuration.GetSection(nameof(Constants)).Get<Constants>();

builder.Services.AddSingleton(constants);

var tradeConfiguration = builder.Configuration.GetSection(nameof(TradeConfiguration)).Get<TradeConfiguration>();

builder.Services.AddSingleton(tradeConfiguration);

if (constants.RunBot)
{
    builder.Services.AddHostedService<TradingService>();
}

var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
    .WaitAndRetryAsync(3, retry => TimeSpan.FromSeconds(Math.Pow(2, retry)));

builder.Services.AddHttpClient<OandaApiService>(httpClient =>
{
    httpClient.BaseAddress = new Uri(constants.OandaApiUrl);

    httpClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, $"Bearer {constants.ApiKey}");

    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

}).AddPolicyHandler(retryPolicy);

builder.Services.AddHttpClient<OandaPricingService>(httpClient =>
{
    httpClient.BaseAddress = new Uri(constants.OandaStreamUrl);

    httpClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, $"Bearer {constants.ApiKey}");

    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

}).AddPolicyHandler(retryPolicy);

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
