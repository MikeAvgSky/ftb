
var builder = WebApplication.CreateBuilder(args);

// Configure Services

var version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;

var constants = builder.Configuration.GetSection(nameof(Constants)).Get<Constants>();

builder.Services.AddSingleton(constants);

var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
    .WaitAndRetryAsync(3, retry => TimeSpan.FromSeconds(Math.Pow(2, retry)));

builder.Services.AddHttpClient<OandaApiService>(httpClient =>
{
    httpClient.BaseAddress = new Uri(constants.OandaUrl);

    httpClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, $"Bearer {constants.ApiKey}");

    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

}).AddPolicyHandler(retryPolicy);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(version, new OpenApiInfo());
});

var app = builder.Build();

// Configure

app.UseSwagger();

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint($"./{version}/swagger.json", "Trading Bot API");
});

app.MapInstrumentEndpoints();

app.Run();
