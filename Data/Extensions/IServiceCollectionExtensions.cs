﻿using Data.Controllers;
using Data.QuoteProvider;
using Data.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Data.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddDataLibraryConfiguration(this IServiceCollection serviceCollection, IConfiguration configuration) => serviceCollection
            .Configure<QuoteRepositorySettings>(configuration.GetSection("QuoteRepositorySettings"))
            .Configure<ReturnRepositorySettings>(configuration.GetSection("ReturnRepositorySettings"))
            .AddTransient<IQuoteRepository, QuoteRepository>()
            .AddTransient<IReturnRepository, ReturnRepository>()
            .AddTransient<IQuoteProvider, YahooFinanceApiQuoteProvider>()
            .AddTransient<QuotesService>()
            .AddTransient<ReturnsService>()
            .AddTransient<IndicesService>();
    }
}
