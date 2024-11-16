using Data.BackTest;
using Data.Extensions;
using Data.Returns;
using Data.SyntheticIndices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

class Program
{
    static async Task Main()
    {
        using var serviceProvider = BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        await RunBackTests(serviceProvider, logger);
        await PrimeReturnsCache(serviceProvider, logger);
    }

    static ServiceProvider BuildServiceProvider()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        return new ServiceCollection()
            .AddLogging(builder => builder.AddConfiguration(configuration.GetSection("Logging")).AddConsole())
            .AddDataLibraryConfiguration(configuration)
            .BuildServiceProvider();
    }

    static async Task RunBackTests(IServiceProvider provider, ILogger<Program> logger)
    {
        var backTestService = provider.GetRequiredService<IBackTestService>();

        var dailyTickers = new List<string>
        {
            /* $^ITSM */    "DFALX,AVDE",
            /* $^ILCB */    "DFALX,AVDE",
            /* $^ILCV */    "DFIVX,AVIV",
            /* $^ILCG */    "EFG",
            /* $^IMCV */    "DFVQX,DXIV",
            /* $^ISCB */    "DFISX,AVDS",
            /* $^ISCV */    "DISVX,AVDV",
            /* $^ISCG */    "DISMX",
            /* $^EMTSM */   "DFEMX,AVEM",
            /* $^EMLCB */   "DFEMX,AVEM",
            /* $^EMLCV */   "DFEVX,AVES",
            /* $^EMLCG */   "XSOE",
            /* $^EMSCB */   "DEMSX,AVEE",
            /* $^EMSCV */   "DGS",
            "$SPYTR,^GSPC,VFINX,VOO",
            "BTC-USD,IBIT",
            "ETH-USD,ETHA"
        };

        var monthlyTickers = new List<string>
        {
            /* $^USTSM */   "$USTSM,VTSMX,VTI,AVUS",
            /* $^USLCB */   "$SPYTR,VFINX,VOO",
            /* $^USLCV */   "$USLCV,DFLVX,AVLV",
            /* $^USLCG */   "$USLCG,VIGAX",
            /* $^USMCB */   "$USMCB,VIMAX,AVMC",
            /* $^USMCV */   "$USMCV,DFVEX,AVMV",
            /* $^USMCG */   "$USMCG,VMGMX",
            /* $^USSCB */   "$USSCB,VSMAX,AVSC",
            /* $^USSCV */   "$USSCV,DFSVX,AVUV",
            /* $^USSCG */   "$USSCG,VSGAX"
        };

        async Task ProcessTickers(List<string> tickers, PeriodType periodType)
        {
            var percentage = Math.Round(100m / tickers.Count, 2, MidpointRounding.ToZero);
            var remainder = 100m - (percentage * tickers.Count);

            await backTestService.GetPortfolioBackTests(
                new List<List<BackTestAllocation>>
                {
                    tickers
                        .Select(ticker => new BackTestAllocation
                        {
                            Percentage = percentage,
                            Ticker = ticker
                        })
                        .Append(new BackTestAllocation
                        {
                            Percentage = remainder,
                            Ticker = "$TBILL"
                        })
                        .ToList()
                },
                periodType: periodType
            );
        }

        await DataRefreshJob.Utils.Timer.Exec("Get stuff", () => Task.WhenAll(
            ProcessTickers(dailyTickers, PeriodType.Daily),
            ProcessTickers(monthlyTickers, PeriodType.Monthly),
            ProcessTickers(monthlyTickers, PeriodType.Yearly)
        ));
    }

    static async Task PrimeReturnsCache(IServiceProvider provider, ILogger<Program> logger)
    {
        var returnsService = provider.GetRequiredService<IReturnsService>();
        var indicesService = provider.GetRequiredService<ISyntheticIndicesService>();

        var periodTypes = Enum.GetValues<PeriodType>().Reverse();
        var syntheticIndexTickers = indicesService.GetSyntheticIndexTickers();
        var tickers = syntheticIndexTickers.Concat([
            "BLOK",
            "FDIG",
            "BITQ",
            "DAPP",
            "BKCH",
            "WGMI",
            "GLD",
            "SGOL",
            "GLDM",
            "DBMF",
            "KMLM",
            "CTA",
            "USFR"
        ]);

        // Concurrently

        var returnsHistoryTasks = new List<Task>();

        foreach (var ticker in tickers)
        {
            foreach (var periodType in periodTypes)
            {
                returnsHistoryTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        return await returnsService.GetReturnsHistory(
                            ticker,
                            periodType,
                            DateTime.MinValue,
                            DateTime.MaxValue);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "{ticker}: {periodType}: {exMessage}", ticker, periodType, ex.Message);

                        return null;
                    }
                }));
            }
        }

        await Task.WhenAll(returnsHistoryTasks);

        // Pause

        Console.WriteLine("\n\nPress Enter key to continue...\n");
        Console.ReadLine();

        // Serially

        var failures = new Dictionary<string, List<(PeriodType, string)>>();

        foreach (var ticker in indicesService.GetSyntheticIndexTickers())
        {
            foreach (var periodType in Enum.GetValues<PeriodType>().Reverse())
            {
                try
                {
                    await returnsService.GetReturnsHistory(
                        ticker,
                        periodType,
                        DateTime.MinValue,
                        DateTime.MaxValue);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{ticker}: {periodType}: {exMessage}", ticker, periodType, ex.Message);

                    if (!failures.TryGetValue(ticker, out List<(PeriodType, string)>? value))
                    {
                        value = ([]);
                        failures[ticker] = value;
                    }

                    value.Add((periodType, ex.Message));
                }
            }
        }

        if (failures.Count == 0)
        {
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Failures:");

        foreach (var pair in failures)
        {
            foreach (var (periodType, message) in pair.Value)
            {
                Console.WriteLine($"{pair.Key}: {periodType}: {(message.Length > 100 ? message[..100] : message)}");
            }
        }
    }
}