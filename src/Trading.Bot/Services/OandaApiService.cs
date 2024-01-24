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
                    value = JsonSerializer.Deserialize<T>(stringResponse,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                    return new ApiResponse<T>(response.StatusCode, value);
                }

                var dictResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(stringResponse);

                value = JsonSerializer.Deserialize<T>(dictResponse[dataKey]?.ToString() ?? "",
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                return new ApiResponse<T>(response.StatusCode, value);
            }

            return new ApiResponse<T>(response.StatusCode, default);
        }
        catch (Exception)
        {
            return new ApiResponse<T>(HttpStatusCode.InternalServerError, default);
        }
    }

    public async Task<ApiResponse<AccountSummary>> GetAccountSummary() =>
        await GetAsync<AccountSummary>($"accounts/{_accountId}/summary", "account");

    public async Task<ApiResponse<List<AccountInstrument>>> GetAccountInstruments(string instruments = default)
    {
        var endpoint = $"accounts/{_accountId}/instruments";

        if (instruments != default)
        {
            endpoint += $"?instruments={instruments}";
        }

        return await GetAsync<List<AccountInstrument>>(endpoint, "instruments");
    }
        

    public async Task<ApiResponse<CandleData>> GetCandleData(string instrument, string granularity = "H1",
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

        return await GetAsync<CandleData>(endpoint);
    }

    public async Task<ApiResponse<List<InstrumentPrice>>> GetPrices(string instruments) =>
        await GetAsync<List<InstrumentPrice>>($"accounts/{_accountId}/pricing?instruments={instruments}");
}