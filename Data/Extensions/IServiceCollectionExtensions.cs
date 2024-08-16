using Data.BackTest;
using Data.Indices;
using Data.Quotes;
using Data.Quotes.QuoteProvider;
using Data.Returns;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Data.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddDataLibraryConfiguration(this IServiceCollection serviceCollection, IConfiguration configuration) => serviceCollection
        .Configure<QuoteRepositoryOptions>(configuration.GetSection("QuoteRepositorySettings"))
        .Configure<ReturnRepositoryOptions>(configuration.GetSection("ReturnRepositorySettings"))
        .AddTransient<IQuoteRepository, QuoteRepository>()
        .AddTransient<IReturnRepository, ReturnRepository>()
        .AddTransient<IQuoteProvider, YahooFinanceApiQuoteProvider>()
        .AddTransient<IQuotesService, QuotesService>()
        .AddTransient<IReturnsService, ReturnsService>()
        .AddTransient<IIndicesService, IndicesService>()
        .AddTransient<IBackTestService, BackTestService>()
        .AddTransient<QuotesService>()
        .AddTransient<ReturnsService>()
        .AddTransient<IndicesService>();
}
