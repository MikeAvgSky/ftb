namespace Trading.Bot.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOandaApiService(this IServiceCollection services, Constants constants)
    {
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, retry => TimeSpan.FromSeconds(Math.Pow(2, retry)));

        services.AddHttpClient<OandaApiService>(httpClient =>
        {
            httpClient.BaseAddress = new Uri(constants.OandaApiUrl);

            httpClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, $"Bearer {constants.ApiKey}");

            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        }).AddPolicyHandler(retryPolicy);

        return services;
    }

    public static IServiceCollection AddOandaStreamService(this IServiceCollection services, Constants constants)
    {
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, retry => TimeSpan.FromSeconds(Math.Pow(2, retry)));

        services.AddHttpClient<OandaStreamService>(httpClient =>
        {
            httpClient.BaseAddress = new Uri(constants.OandaStreamUrl);

            httpClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, $"Bearer {constants.ApiKey}");

            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        }).AddPolicyHandler(retryPolicy);

        return services;
    }
}