namespace Trading.Bot.Mediator;

public sealed class CalculateStrategyResultHandler : IRequestHandler<CalculateStrategyResultRequest, IResult>
{
    public Task<IResult> Handle(CalculateStrategyResultRequest request, CancellationToken cancellationToken)
    {
        var strategy = (request.Strategy switch
        {
            StrategyName.MovingAverage => request.File.GetObjectFromCsv<MovingAverageCross>(),
            _ => Enumerable.Empty<Strategy>()
        }).ToList();

        if (!strategy.Any()) return Task.FromResult(Results.BadRequest("Strategy is not valid"));

        var filename = request.File.FileName[..request.File.FileName.LastIndexOf('.')];

        var result = new StrategyResult
        {
            TradeCount = strategy.Count(s => s.Trade != Trade.None),
            TotalGain = strategy.Select(s => s.Gain).Sum(),
            MeanGain = strategy.Select(s => s.Gain).Average(),
            MinGain = strategy.OrderBy(s => s.Gain).First().Gain,
            MaxGain = strategy.OrderByDescending(s => s.Gain).First().Gain
        };

        return Task.FromResult(request.Download
            ? Results.File(new List<StrategyResult>{ result }.GetCsvBytes(),
                "text/csv", $"{filename}_Result.csv")
            : Results.Ok(result));
    }
}

public record CalculateStrategyResultRequest : IHttpRequest
{
    public IFormFile File { get; set; }
    public StrategyName Strategy { get; set; }
    public bool Download { get; set; }
}