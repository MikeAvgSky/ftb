namespace Trading.Bot.Mediator;

public sealed class StrategyResultHandler : IRequestHandler<StrategyResultRequest, IResult>
{
    internal const string FilenameRegex =
        @"^(?<instrument>[A-Z_-]{7})_(?<granularity>[A-Z0-9]{2})_(?<strategy>[A-Z0-9_-]+).csv";

    public Task<IResult> Handle(StrategyResultRequest request, CancellationToken cancellationToken)
    {
        var results = new List<IndicatorResult>();

        var filenameRegex = new Regex(FilenameRegex, RegexOptions.IgnoreCase);

        foreach (var file in request.Files)
        {
            var match = filenameRegex.Match(file.FileName);

            if (!match.Success) continue;

            var strategy = (match.Groups["strategy"].Value switch
            {
                var s when s.StartsWith("MA") => file.GetObjectFromCsv<MacResult>(),
                var s when s.StartsWith("BB") => file.GetObjectFromCsv<BollingerBandsResult>(),
                _ => Enumerable.Empty<Indicator>()
            }).ToList();

            if (!strategy.Any()) return Task.FromResult(Results.BadRequest("Strategy is not valid"));

            var result = new IndicatorResult
            {
                Instrument = match.Groups["instrument"].Value,
                Granularity = match.Groups["granularity"].Value,
                Strategy = match.Groups["strategy"].Value,
                TradeCount = strategy.Count(s => s.Signal != Signal.None),
                TotalGain = strategy.Select(s => s.Gain).Sum(),
                MeanGain = strategy.Select(s => s.Gain).Average(),
                MinGain = strategy.OrderBy(s => s.Gain).First().Gain,
                MaxGain = strategy.OrderByDescending(s => s.Gain).First().Gain
            };

            results.Add(result);
        }

        return Task.FromResult(request.Download
            ? Results.File(results.OrderByDescending(r => r.TotalGain).GetCsvBytes(),
                "text/csv", "results.csv")
            : Results.Ok(results));
    }
}

public record StrategyResultRequest : IHttpRequest
{
    public IFormFileCollection Files { get; set; }
    public bool Download { get; set; }
}