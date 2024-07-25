public static class FundHistoryCacheController
{
    public static async Task RefreshFundHistoryCache(FundHistoryRepository cache, HashSet<string> tickers)
    {
        foreach (var ticker in tickers)
        {
            Console.WriteLine();

            var fundHistory = await cache.Get(ticker);

            if (fundHistory == null)
            {
                Console.WriteLine($"{ticker}: No history found in cache.");
            }
            else
            {
                Console.WriteLine($"{ticker}: {fundHistory.Prices.Count} record(s) in cache, {fundHistory.Prices[0].DateTime:yyyy-MM-dd} to {fundHistory.Prices[fundHistory.Prices.Count - 1].DateTime:yyyy-MM-dd}.");
            }

            FundHistory? missingFundHistory;

            if (fundHistory == null)
            {
                Console.WriteLine($"{ticker}: Download entire history.");

                missingFundHistory = await FundHistoryDownloader.GetHistoryAll(ticker);
            }
            else
            {
                missingFundHistory = await FundHistoryDownloader.GetMissingHistory(fundHistory, out DateTime missingStart, out DateTime missingEnd);

                if (missingFundHistory != null)
                {
                    Console.WriteLine($"{ticker}: Missing history identified as {missingStart:yyyy-MM-dd} to {missingEnd:yyyy-MM-dd}");
                }
            }

            if (missingFundHistory == null)
            {
                Console.WriteLine($"{ticker}: No history missing.");
                continue;
            }

            Console.WriteLine($"{ticker}: Save {missingFundHistory.Prices.Count} new record(s), {missingFundHistory.Prices[0].DateTime:yyyy-MM-dd} to {missingFundHistory.Prices[missingFundHistory.Prices.Count - 1].DateTime:yyyy-MM-dd}.");

            await cache.Put(missingFundHistory);
        }
    }
}