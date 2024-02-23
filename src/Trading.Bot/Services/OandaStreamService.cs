namespace Trading.Bot.Services;

public class OandaStreamService
{
    private readonly HttpClient _httpClient;
    private readonly string _accountId;
    public readonly ConcurrentDictionary<string, LivePrice> _livePrices = new();

    public OandaStreamService(HttpClient httpClient, Constants constants)
    {
        _httpClient = httpClient;
        _accountId = constants.AccountId;
    }

    public async Task StreamLivePrices(string instruments)
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

            _livePrices[price.Instrument] = new LivePrice(price);
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