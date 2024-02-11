namespace Trading.Bot.Mediator;

public sealed class IndicatorResultHandler : IRequestHandler<IndicatorResultRequest, IResult>
{
    internal const string FilenameRegex =
        @"^(?<instrument>[A-Z_-]{7})_(?<granularity>[A-Z0-9]{2})_(?<indicator>[A-Z0-9_-]+).csv";

    public Task<IResult> Handle(IndicatorResultRequest request, CancellationToken cancellationToken)
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
                var s when s.Contains("RSI") && s.Contains("EMA") => file.GetObjectFromCsv<RsiEmaResult>(),
                _ => Enumerable.Empty<Indicator>()
            }).ToList();

            if (!strategy.Any()) return Task.FromResult(Results.BadRequest("Indicator is not valid"));

            var result = new IndicatorResult
            {
                Instrument = match.Groups["instrument"].Value,
                Granularity = match.Groups["granularity"].Value,
                Indicator = match.Groups["indicator"].Value,
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

public record IndicatorResultRequest : IHttpRequest
{
    public IFormFileCollection Files { get; set; }
    public bool Download { get; set; }
}