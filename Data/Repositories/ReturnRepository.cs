using System.Globalization;
using Data.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Data.Repositories
{
    internal class ReturnRepository : IReturnRepository
    {
        private readonly string cachePath;

        private readonly string syntheticReturnsFilePath;

        private readonly ILogger<ReturnRepository> logger;

        public ReturnRepository(IOptions<ReturnRepositorySettings> settings, ILogger<ReturnRepository> logger)
        {
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(logger);

            cachePath = settings.Value.CacheDirPath;

            if (!Directory.Exists(cachePath))
            {
                Directory.CreateDirectory(cachePath);
            }

            syntheticReturnsFilePath = settings.Value.SyntheticReturnsFilePath;

            if (!File.Exists(syntheticReturnsFilePath))
            {
                throw new ArgumentException($"{syntheticReturnsFilePath} file does not exist.", nameof(settings));
            }

            this.logger = logger;
        }

        public bool Has(string ticker, PeriodType period)
        {
            ArgumentNullException.ThrowIfNull(ticker);
            ArgumentNullException.ThrowIfNull(period);

            return Has(ticker, period, out string _);
        }

        private bool Has(string ticker, PeriodType period, out string cacheFilePath)
        {
            cacheFilePath = GetCsvFilePath(ticker, period);

            return File.Exists(cacheFilePath);
        }

        public async Task<List<PeriodReturn>> Get(
            string ticker,
            PeriodType periodType,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            ArgumentNullException.ThrowIfNull(ticker);
            ArgumentNullException.ThrowIfNull(periodType);

            if (!Has(ticker, periodType, out string csvFilePath))
            {
                throw new KeyNotFoundException($"No record for {nameof(ticker)} \"{ticker}\".");
            }

            var csvLines = await File.ReadAllLinesAsync(csvFilePath);

            return csvLines
                .Select(PeriodReturn.ParseCsvLine)
                .Where(pair =>
                    (startDate == null || pair.PeriodStart >= startDate) &&
                    (endDate == null || pair.PeriodStart <= endDate))
                .ToList();
        }

        public Task Put(string ticker, IEnumerable<PeriodReturn> returns, PeriodType period)
        {
            ArgumentNullException.ThrowIfNull(ticker);
            ArgumentNullException.ThrowIfNull(returns);
            ArgumentNullException.ThrowIfNull(period);

            if (!returns.Any())
            {
                throw new ArgumentException("Cannot be empty.", nameof(returns));
            }

            var csvFilePath = GetCsvFilePath(ticker, period);
            var csvDirPath = Path.GetDirectoryName(csvFilePath);

            if (!Directory.Exists(csvDirPath))
            {
                Directory.CreateDirectory(csvDirPath!);
            }

            var csvFileLines = returns.Select(r => r.ToCsvLine());

            return File.WriteAllLinesAsync(csvFilePath, csvFileLines);
        }

        // TODO test
        public async Task<Dictionary<string, List<PeriodReturn>>> GetSyntheticMonthlyReturns()
        {
            var columnIndexToCategory = new Dictionary<int, string>
            {
                [1] = "$TSM",
                [3] = "$LCB",
                [4] = "$LCV",
                [5] = "$LCG",
                [6] = "$MCB",
                [7] = "$MCV",
                [8] = "$MCG",
                [9] = "$SCB",
                [10] = "$SCV",
                [11] = "$SCG"
            };

            const int headerLinesCount = 1;
            const int dateColumnIndex = 0;

            var returns = new Dictionary<string, List<PeriodReturn>>();
            var fileLines = await File.ReadAllLinesAsync(syntheticReturnsFilePath);
            var fileLinesSansHeader = fileLines.Skip(headerLinesCount);

            foreach (var line in fileLinesSansHeader)
            {
                var cells = line.Split(',');
                var date = DateTime.Parse(cells[dateColumnIndex]);

                foreach (var (cellIndex, ticker) in columnIndexToCategory)
                {
                    var cellValue = decimal.Parse(cells[cellIndex], NumberStyles.Any, CultureInfo.InvariantCulture);

                    if (!returns.TryGetValue(ticker, out var tickerReturns))
                    {
                        tickerReturns = returns[ticker] = [];
                    }

                    // decimal.ToString("G29") will trim trailing 0s
                    tickerReturns.Add(new PeriodReturn()
                    {
                        PeriodStart = date,
                        ReturnPercentage = decimal.Parse($"{cellValue:G29}"),
                        SourceTicker = ticker,
                        PeriodType = PeriodType.Monthly
                    });
                }
            }

            return returns;
        }

        // TODO test
        public async Task<Dictionary<string, List<PeriodReturn>>> GetSyntheticYearlyReturns()
        {
            static List<PeriodReturn> CalculateYearlyFromMonthly(string ticker, List<PeriodReturn> monthlyReturns)
            {
                var result = new List<PeriodReturn>();

                var yearStarts = monthlyReturns
                    .GroupBy(r => r.PeriodStart.Year)
                    .Select(g => g.OrderBy(r => r.PeriodStart).First())
                    .OrderBy(r => r.PeriodStart)
                    .ToArray();

                var i = yearStarts[0].PeriodStart.Month == 1 ? 0 : 1;

                for (; i < yearStarts.Length; i++)
                {
                    var currentYear = yearStarts[i].PeriodStart.Year;
                    var currentYearMonthlyReturns = monthlyReturns.Where(r => r.PeriodStart.Year == currentYear);
                    var currentYearAggregateReturn = currentYearMonthlyReturns.Aggregate(1.0m, (acc, item) => acc * (1 + item.ReturnPercentage / 100)) - 1;
                    var periodReturn = new PeriodReturn()
                    {
                        PeriodStart = new DateTime(currentYear, 1, 1),
                        ReturnPercentage = currentYearAggregateReturn,
                        SourceTicker = ticker,
                        PeriodType = PeriodType.Yearly
                    };

                    result.Add(periodReturn);
                }

                return result;
            }

            var monthlyReturns = await GetSyntheticMonthlyReturns();
            var yearlyReturns = monthlyReturns.ToDictionary(pair => pair.Key, pair => CalculateYearlyFromMonthly(pair.Key, pair.Value));

            return yearlyReturns;
        }

        private string GetCsvFilePath(string ticker, PeriodType period)
        {
            return Path.Combine(cachePath, $"./{period.ToString().ToLowerInvariant()}/{ticker}.csv");
        }
    }
}