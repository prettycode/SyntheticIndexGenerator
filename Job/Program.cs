using Data.Controllers;
using Data.Repositories;
using Job;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Timer = Job.Utils.Timer;

static ILogger<T> CreateLogger<T>() where T : class => LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<T>();

var settings = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .Build()
    .Get<AppSettings>() ?? throw new ApplicationException("Settings malformed.");

var quoteCache = new QuoteRepository(settings.QuoteRepositoryDataPath, CreateLogger<QuoteRepository>());
var returnCache = new ReturnRepository(settings.ReturnRepositoryDataPath, settings.SyntheticReturnsFilePath);

var quotesManager = new QuotesManager(quoteCache, CreateLogger<QuotesManager>());
var returnsManager = new ReturnsManager(quoteCache, returnCache, CreateLogger<ReturnsManager>());
var indicesManager = new IndicesManager(returnCache, CreateLogger<IndicesManager>());

var quoteTickersNeeded = IndicesManager.GetBackfillTickers();

await Timer.Exec("Refresh quotes", quotesManager.RefreshQuotes(quoteTickersNeeded));
await Timer.Exec("Refresh returns", returnsManager.RefreshReturns());
await Timer.Exec("Refresh indices", indicesManager.RefreshIndices());