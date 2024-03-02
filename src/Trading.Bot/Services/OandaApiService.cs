namespace Trading.Bot.Services;

public class OandaApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OandaApiService> _logger;
    private readonly string _accountId;
    public const string DefaultGranularity = "H1";
    public const string DefaultPrice = "MBA";

    public OandaApiService(HttpClient httpClient, ILogger<OandaApiService> logger, Constants constants)
    {
        _httpClient = httpClient;
        _logger = logger;
        _accountId = constants.AccountId;
    }

    private async Task<ApiResponse<T>> GetAsync<T>(string endpoint, string dataKey = default) where T : class
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                return await HandleApiResponse<T>(dataKey, response);
            }

            return new ApiResponse<T>(response.StatusCode, default);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred while getting data from {endpoint}");

            return new ApiResponse<T>(HttpStatusCode.InternalServerError, default);
        }
    }

    private async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object body = default, string dataKey = default) where T : class
    {
        try
        {
            HttpResponseMessage response;

            if (body == default)
            {
                response = await _httpClient.PostAsync(endpoint, new StringContent(string.Empty));
            }
            else
            {
                var content = Serialize(body);

                response = await _httpClient.PostAsync(endpoint, content);
            }
            
            if (response.IsSuccessStatusCode)
            {
                return await HandleApiResponse<T>(dataKey, response);
            }

            return new ApiResponse<T>(response.StatusCode, default);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred while posting {Serialize(body)} to {endpoint}");

            return new ApiResponse<T>(HttpStatusCode.InternalServerError, default);
        }
    }

    private async Task<ApiResponse<T>> PutAsync<T>(string endpoint, object body = default, string dataKey = default) where T : class
    {
        try
        {
            HttpResponseMessage response;

            if (body == default)
            {
                response = await _httpClient.PutAsync(endpoint, new StringContent(string.Empty));
            }
            else
            {
                var content = Serialize(body);

                response = await _httpClient.PutAsync(endpoint, content);
            }

            if (response.IsSuccessStatusCode)
            {
                return await HandleApiResponse<T>(dataKey, response);
            }

            return new ApiResponse<T>(response.StatusCode, default);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred while updating {Serialize(body)} from {endpoint}");

            return new ApiResponse<T>(HttpStatusCode.InternalServerError, default);
        }
    }

    private static StringContent Serialize(object body)
    {
        var content =
            new StringContent(
                JsonSerializer.Serialize(body,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    }),
                Encoding.UTF8, "application/json");

        return content;
    }

    private static async Task<ApiResponse<T>> HandleApiResponse<T>(string dataKey, HttpResponseMessage response) where T : class
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

    private static T Deserialize<T>(string stringResponse) where T : class
    {
        return JsonSerializer.Deserialize<T>(stringResponse,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            });
    }

    public async Task<AccountResponse> GetAccountSummary()
    {
        var endpoint = $"accounts/{_accountId}/summary";

        var response = await GetAsync<AccountResponse>(endpoint, "account");

        return response.StatusCode == HttpStatusCode.OK
            ? response.Value
            : null;
    }

    public async Task<Price[]> GetPrices(string instruments)
    {
        var endpoint = $"accounts/{_accountId}/pricing?instruments={instruments}&includeHomeConversions=true";

        var response = await GetAsync<PricingResponse>(endpoint);

        return response.StatusCode == HttpStatusCode.OK
            ? response.Value.MapToPrices()
            : Array.Empty<Price>();
    }

    public async Task<Instrument[]> GetInstruments(string instruments)
    {
        var endpoint = BuildInstrumentsEndpoint(instruments);

        var response = await GetAsync<InstrumentResponse[]>(endpoint, "instruments");

        return response.StatusCode == HttpStatusCode.OK
            ? response.Value.MapToInstruments()
            : Array.Empty<Instrument>();
    }

    public async Task<Candle[]> GetCandles(string instrument, string granularity = default,
        string price = default, int count = 500, DateTime fromDate = default, DateTime toDate = default)
    {
        var endpoint = BuildCandlesEndpoint(instrument, granularity, price, count, 
            fromDate, toDate);

        var response =  await GetAsync<CandleResponse>(endpoint);

        return response.StatusCode == HttpStatusCode.OK
            ? response.Value.Candles.MapToCandles()
            : Array.Empty<Candle>();
    }

    public async Task<DateTime> GetLastCandleTime(string instrument, string granularity = default)
    {
        var endpoint = BuildCandlesEndpoint(instrument, granularity, count: 10);

        var response = await GetAsync<CandleResponse>(endpoint);

        return response.StatusCode == HttpStatusCode.OK
            ? response.Value.Candles.Last(c => c.Complete).Time
            : default;
    }

    public async Task<OrderFilledResponse> PlaceTrade(Order orderRequest)
    {
        var endpoint = $"accounts/{_accountId}/orders";

        var response = await PostAsync<OrderFilledResponse>(endpoint, orderRequest, "orderFillTransaction");

        return response.StatusCode == HttpStatusCode.OK
            ? response.Value
            : null;
    }

    public async Task<bool> CloseTrade(string tradeId)
    {
        var endpoint = $"accounts/{_accountId}/trades/{tradeId}/close";

        var response = await PutAsync<OrderFilledResponse>(endpoint, "orderFillTransaction");

        return response.StatusCode == HttpStatusCode.OK && response.Value is not null;
    }

    public async Task<TradeResponse[]> GetOpenTrades()
    {
        var endpoint = $"accounts/{_accountId}/openTrades";

        var response = await GetAsync<TradeResponse[]>(endpoint, "trades");

        return response.StatusCode == HttpStatusCode.OK
            ? response.Value
            : Array.Empty<TradeResponse>();
    }

    public async Task<TradeResponse> GetTrade(string tradeId)
    {
        var endpoint = $"accounts/{_accountId}/trades/{tradeId}";

        var response = await GetAsync<TradeResponse>(endpoint, "trade");

        return response.StatusCode == HttpStatusCode.OK
            ? response.Value
            : null;
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

    private static string BuildCandlesEndpoint(string instrument, string granularity = default, 
        string price = default, int count = 500, DateTime fromDate = default, DateTime toDate = default)
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
}