namespace Trading.Bot.Models;

public class Strategy
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Trade Trade { get; set; }
    public double Diff { get; set; }
    public double Gain { get; set; }
    public double CumGain { get; set; }
}