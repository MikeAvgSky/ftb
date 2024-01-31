namespace Trading.Bot.Services;

public class OandaApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _accountId;
    public const string DefaultGranularity = "H1";
    public const string DefaultPrice = "MBA";

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

    public async Task<ApiResponse<AccountResponse>> GetOandaAccountSummary() =>
        await GetAsync<AccountResponse>($"accounts/{_accountId}/summary", "account");

    public async Task<IEnumerable<Instrument>> GetInstrumentsFromOanda(string instruments)
    {
        var endpoint = BuildInstrumentsEndpoint(instruments);

        var instrumentResponse = await GetAsync<List<InstrumentResponse>>(endpoint, "instruments");

        return instrumentResponse.StatusCode == HttpStatusCode.OK
            ? instrumentResponse.Value.Select(MapToInstrument)
            : Enumerable.Empty<Instrument>();
    }

    private string BuildInstrumentsEndpoint(string instruments)
    {
        var endpoint = $"accounts/{_accountId}/instruments";

        if (instruments != default)
        {
            endpoint += $"?instruments={instruments}";
        }

        return endpoint;
    }

    public async Task<IEnumerable<Candle>> GetCandlesFromOanda(string instrument, string granularity,
        string price, int count, DateTime fromDate, DateTime toDate)
    {
        var endpoint = BuildCandlesEndpoint(instrument, granularity, price, count, fromDate, toDate);

        var candleResponse =  await GetAsync<CandleResponse>(endpoint);

        return candleResponse.StatusCode == HttpStatusCode.OK
            ? candleResponse.Value.Candles.Where(c => c.Complete).Select(MapToCandle)
            : Enumerable.Empty<Candle>();
    }

    private static string BuildCandlesEndpoint(string instrument, string granularity, string price, int count,
        DateTime fromDate, DateTime toDate)
    {
        var endpoint = $"instruments/{instrument}/candles";

        if (!string.IsNullOrEmpty(granularity))
        {
            endpoint += $"?granularity={granularity}";
        }
        else
        {
            endpoint += $"?granularity={DefaultGranularity}";
        }

        if (!string.IsNullOrEmpty(price))
        {
            endpoint += $"&price={price}";
        }
        else
        {
            endpoint += $"&price={DefaultPrice}";
        }

        if (fromDate != default && toDate != default)
        {
            endpoint += $"&from={fromDate:O}&to{toDate:O}&count=5000";
        }
        else
        {
            endpoint += $"&count={count}";
        }

        return endpoint;
    }

    public async Task<ApiResponse<List<PricingResponse>>> GetPricesFromOanda(string instruments) =>
        await GetAsync<List<PricingResponse>>($"accounts/{_accountId}/pricing?instruments={instruments}");

    private static Candle MapToCandle(CandleData candleData)
    {
        return new Candle(candleData);
    }

    private static Instrument MapToInstrument(InstrumentResponse ir)
    {
        return new Instrument(ir.Name, ir.Type, ir.DisplayName, ir.PipLocation, ir.TradeUnitsPrecision, ir.MarginRate);
    }
}