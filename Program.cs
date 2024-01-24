
var builder = WebApplication.CreateBuilder(args);

// Configure Services

var constants = builder.Configuration.GetSection(nameof(Constants)).Get<Constants>();

builder.Services.AddSingleton(constants);

var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
    .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
    .WaitAndRetryAsync(3, retry => TimeSpan.FromSeconds(Math.Pow(2, retry)));

builder.Services.AddHttpClient<OandaApiService>(httpClient =>
{
    httpClient.BaseAddress = new Uri(constants.OandaUrl);

    httpClient.DefaultRequestHeaders.Clear();

    httpClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, $"Bearer {constants.ApiKey}");

    httpClient.DefaultRequestHeaders.Add(HeaderNames.ContentType, "application/json");

}).AddPolicyHandler(retryPolicy);

var app = builder.Build();

// Configure

app.Run();
