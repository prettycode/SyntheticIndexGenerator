using Data.BackTest;
using Data.Quotes;
using Data.Quotes.QuoteProvider;
using Data.Returns;
using Data.SyntheticIndices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Data.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddDataLibraryConfiguration(this IServiceCollection serviceCollection, IConfiguration configuration) => serviceCollection
        .Configure<QuoteRepositoryOptions>(configuration.GetSection("QuoteRepositoryOptions"))
        .Configure<ReturnRepositoryOptions>(configuration.GetSection("ReturnRepositoryOptions"))
        .AddTransient<IQuoteRepository, QuoteRepository>()
        .AddTransient<IReturnRepository, ReturnRepository>()
        .AddTransient<IQuoteProvider, YahooFinanceApiQuoteProvider>()
        .AddTransient<IQuotesService, QuotesService>()
        .AddTransient<IReturnsService, ReturnsService>()
        .AddTransient<ISyntheticIndicesService, SyntheticIndicesService>()
        .AddTransient<IBackTestService, BackTestService>()
        .AddTransient<QuotesService>()
        .AddTransient<ReturnsService>()
        .AddTransient<SyntheticIndicesService>();
}
