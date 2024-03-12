namespace Trading.Bot.Services;

public class OandaStreamService
{
    private readonly HttpClient _httpClient;
    private readonly LiveTradeCache _liveTradeCache;
    private readonly ILogger<OandaStreamService> _logger;
    private readonly string _accountId;

    public OandaStreamService(HttpClient httpClient, LiveTradeCache liveTradeCache,
        ILogger<OandaStreamService> logger, Constants constants)
    {
        _httpClient = httpClient;
        _liveTradeCache = liveTradeCache;
        _logger = logger;
        _accountId = constants.AccountId;
    }

    public async Task StreamLivePrices(string instruments, CancellationToken stoppingToken)
    {
        try
        {
            var endpoint = $"accounts/{_accountId}/pricing/stream?instruments={instruments}";

            var request = new HttpRequestMessage(HttpMethod.Get, endpoint);

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, stoppingToken);

            response.EnsureSuccessStatusCode();

            var responseStream = await response.Content.ReadAsStreamAsync(stoppingToken);

            using var reader = new StreamReader(responseStream);

            while (!reader.EndOfStream && !stoppingToken.IsCancellationRequested)
            {
                var stringResponse = await reader.ReadLineAsync(stoppingToken);

                var price = Deserialize<PriceResponse>(stringResponse);

                if (price is not null && price.Type == "PRICE" && price.Tradeable)
                {
                    _liveTradeCache.AddToDictionary(new LivePrice(price));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while trying to stream live prices. Stopping service.");
        }
    }

    private static T Deserialize<T>(string stringResponse) where T : class
    {
        return JsonSerializer.Deserialize<T>(stringResponse,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            });
    }
}