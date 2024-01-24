namespace Trading.Bot.Models;

public class ApiResponse<T>
{
    public ApiResponse(HttpStatusCode statusCode, T value)
    {
        StatusCode = statusCode;
        Value = value;
    }

    public HttpStatusCode StatusCode { get; }
    public T Value { get; }
}