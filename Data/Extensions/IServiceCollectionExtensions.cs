using Data.BackTest;
using Data.Quotes;
using Data.Quotes.QuoteProvider;
using Data.Returns;
using Data.SyntheticIndices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Data.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddDataLibraryConfiguration(this IServiceCollection serviceCollection, IConfiguration configuration) => serviceCollection
        .Configure<QuoteRepositoryOptions>(configuration.GetSection("QuoteRepositoryOptions"))
        .Configure<ReturnsCacheOptions>(configuration.GetSection("ReturnsCacheOptions"))
        .Configure<QuotesServiceOptions>(configuration.GetSection("QuotesServiceOptions"))
        .Configure<FmpQuoteProviderOptions>(configuration.GetSection("FmpQuoteProviderOptions"))
        .AddTransient<IQuoteRepository, QuoteRepository>()
        .AddSingleton<IReturnsCache>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ReturnsCacheOptions>>();
            var logger = sp.GetRequiredService<ILogger<ReturnsCache>>();

            return ReturnsCache.Create(options, logger).GetAwaiter().GetResult();
        })
        .AddTransient<IQuoteProvider, YahooFinanceChartProvider>()
        //.AddTransient<IQuoteProvider, FmpQuoteProvider>()
        .AddTransient<IQuotesService, QuotesService>()
        .AddTransient<IReturnsService, ReturnsService>()
        .AddTransient<ISyntheticIndicesService, SyntheticIndicesService>()
        .AddTransient<IBackTestService, BackTestService>()
        .AddTransient<QuotesService>()
        .AddTransient<ReturnsService>()
        .AddTransient<SyntheticIndicesService>();
}