public static class SyntheticReturnsController
{
    public static async Task RefreshSyntheticReturns(ReturnsRepository returnsCache)
    {
        ArgumentNullException.ThrowIfNull(returnsCache);

        var indexReturns = await returnsCache.GetSyntheticMonthlyReturns();

        await Task.WhenAll(indexReturns.Select(r => returnsCache.Put(r.Key, r.Value, ReturnPeriod.Monthly)));
    }
}