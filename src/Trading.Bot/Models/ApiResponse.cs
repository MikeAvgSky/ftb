namespace Trading.Bot.Models;

public class ApiResponse<T>
{
    public HttpStatusCode StatusCode { get; }
    public T Value { get; }

    public ApiResponse(HttpStatusCode statusCode, T value)
    {
        StatusCode = statusCode;
        Value = value;
    }
}