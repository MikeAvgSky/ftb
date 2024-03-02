namespace Trading.Bot.Services;

public class OandaStreamService
{
    private readonly HttpClient _httpClient;
    private readonly LivePriceCache _livePriceCache;
    private readonly ILogger<OandaStreamService> _logger;
    private readonly string _accountId;

    public OandaStreamService(HttpClient httpClient, LivePriceCache livePriceCache, 
        ILogger<OandaStreamService> logger, Constants constants)
    {
        _httpClient = httpClient;
        _livePriceCache = livePriceCache;
        _logger = logger;
        _accountId = constants.AccountId;
    }

    public async Task StreamLivePrices(string instruments)
    {
        try
        {
            var endpoint = $"accounts/{_accountId}/pricing/stream?instruments={instruments}";

            var request = new HttpRequestMessage(HttpMethod.Get, endpoint);

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            var responseStream = await response.Content.ReadAsStreamAsync();

            using var reader = new StreamReader(responseStream);

            while (!reader.EndOfStream)
            {
                var stringResponse = await reader.ReadLineAsync();

                var price = Deserialize<PriceResponse>(stringResponse);

                if (price is not null && price.Type == "PRICE")
                {
                    _livePriceCache.AddToDictionary(new LivePrice(price));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while trying to stream live prices. Stopping service.");

            Environment.Exit(0);
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