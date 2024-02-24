namespace Trading.Bot.Services;

public class OandaPricingService
{
    private readonly HttpClient _httpClient;
    private readonly OandaApiService _apiService;
    private readonly string _accountId;
    public readonly ConcurrentDictionary<string, List<LivePrice>> LivePrices = new();

    public OandaPricingService(HttpClient httpClient, OandaApiService apiService, Constants constants)
    {
        _httpClient = httpClient;
        _apiService = apiService;
        _accountId = constants.AccountId;
    }

    public async Task StreamLivePrices(string instruments)
    {
        try
        {
            var inst = await _apiService.GetInstruments(instruments);

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

                var precision = inst.First(x => x.Name == price.Instrument).DisplayPrecision;

                LivePrices[price.Instrument].Add(new LivePrice(price, precision));
            }
        }
        catch (Exception)
        {
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