namespace Trading.Bot.Mediator;

public sealed class CalculateStrategyResultHandler : IRequestHandler<CalculateStrategyResultRequest, IResult>
{
    internal const string FilenameRegex =
        @"^(?<instrument>[A-Z0-9_-]{7})\_(?<strategy>[A-Z0-9_-]+).csv";

    public Task<IResult> Handle(CalculateStrategyResultRequest request, CancellationToken cancellationToken)
    {
        var results = new List<StrategyResult>();

        var filenameRegex = new Regex(FilenameRegex, RegexOptions.IgnoreCase);

        foreach (var file in request.Files)
        {
            var filename = file.FileName[..file.FileName.LastIndexOf('.')];

            var match = filenameRegex.Match(filename);

            if (!match.Success) continue;

            var strategy = (match.Groups["strategy"].Value switch
            {
                var s when s.StartsWith("MA") => file.GetObjectFromCsv<MovingAverageCross>(),
                _ => Enumerable.Empty<Strategy>()
            }).ToList();

            if (!strategy.Any()) return Task.FromResult(Results.BadRequest("Strategy is not valid"));

            var result = new StrategyResult
            {
                Instrument = match.Groups["instrument"].Value,
                Strategy = match.Groups["strategy"].Value,
                TradeCount = strategy.Count(s => s.Trade != Trade.None),
                TotalGain = strategy.Select(s => s.Gain).Sum(),
                MeanGain = strategy.Select(s => s.Gain).Average(),
                MinGain = strategy.OrderBy(s => s.Gain).First().Gain,
                MaxGain = strategy.OrderByDescending(s => s.Gain).First().Gain
            };

            results.Add(result);
        }

        return Task.FromResult(request.Download
            ? Results.File(results.OrderByDescending(r => r.MaxGain).GetCsvBytes(),
                "text/csv", "results.csv")
            : Results.Ok(results));
    }
}

public record CalculateStrategyResultRequest : IHttpRequest
{
    public IFormFileCollection Files { get; set; }
    public bool Download { get; set; }
}