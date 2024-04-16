namespace Trading.Bot.Services;

public class OandaStreamService
{
    private readonly ILogger<OandaStreamService> _logger;
    private readonly HttpClient _httpClient;
    private readonly LiveTradeCache _liveTradeCache;
    private readonly string _accountId;

    public OandaStreamService(ILogger<OandaStreamService> logger, HttpClient httpClient,
        LiveTradeCache liveTradeCache, Constants constants)
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

            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, stoppingToken);

            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync(stoppingToken);

            using var reader = new StreamReader(responseStream);

            while (!reader.EndOfStream && !stoppingToken.IsCancellationRequested)
            {
                var stringResponse = await reader.ReadLineAsync(stoppingToken);

                var price = Deserialize<PriceResponse>(stringResponse);

                if (price is null || price.Type != "PRICE") continue;

                if (price.Tradeable)
                    _liveTradeCache.LivePrices[price.Instrument] = new LivePrice(price);
                else
                    _liveTradeCache.LivePrices.Remove(price.Instrument);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while trying to stream live prices");
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