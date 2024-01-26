namespace Trading.Bot.Services;

public class OandaApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _accountId;

    public OandaApiService(HttpClient httpClient, Constants constants)
    {
        _httpClient = httpClient;
        _accountId = constants.AccountId;
    }

    private async Task<ApiResponse<T>> GetAsync<T>(string endpoint, string dataKey = default) where T : class
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                var stringResponse = await response.Content.ReadAsStringAsync();

                T value;

                if (dataKey == default)
                {
                    value = Deserialize<T>(stringResponse);

                    return new ApiResponse<T>(response.StatusCode, value);
                }

                var dictResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(stringResponse);

                if (dictResponse.ContainsKey(dataKey))
                {
                    value = Deserialize<T>(JsonSerializer.Serialize(dictResponse[dataKey]));

                    return new ApiResponse<T>(response.StatusCode, value);
                }

                return new ApiResponse<T>(HttpStatusCode.NotFound, default);
            }

            return new ApiResponse<T>(response.StatusCode, default);
        }
        catch (Exception)
        {
            return new ApiResponse<T>(HttpStatusCode.InternalServerError, default);
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

    public async Task<ApiResponse<AccountResponse>> GetAccountSummary() =>
        await GetAsync<AccountResponse>($"accounts/{_accountId}/summary", "account");

    public async Task<ApiResponse<List<InstrumentResponse>>> GetInstruments(string instruments = default)
    {
        var endpoint = $"accounts/{_accountId}/instruments";

        if (instruments != default)
        {
            endpoint += $"?instruments={instruments}";
        }

        return await GetAsync<List<InstrumentResponse>>(endpoint, "instruments");
    }
        

    public async Task<ApiResponse<CandleResponse>> GetCandles(string instrument, string granularity = "H1",
        string price = "MBA", int count = 10, DateTime fromDate = default, DateTime toDate = default)
    {
        var endpoint = $"instruments/{instrument}/candles?granularity={granularity}&price={price}";

        if (fromDate != default && toDate != default)
        {
            endpoint += $"&from={fromDate:O}&to{toDate:O}";
        }
        else
        {
            endpoint += $"&count={count}";
        }

        return await GetAsync<CandleResponse>(endpoint);
    }

    public async Task<ApiResponse<List<PricingResponse>>> GetPrices(string instruments) =>
        await GetAsync<List<PricingResponse>>($"accounts/{_accountId}/pricing?instruments={instruments}");
}