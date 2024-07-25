public static class SyntheticReturnsController
{
    public static async Task RefreshSyntheticReturns(ReturnsRepository returnsCache)
    {
        var indexReturns = await returnsCache.GetSyntheticMonthlyReturns();

        await Task.WhenAll(indexReturns.Select(r => returnsCache.Put(r.Key, r.Value, ReturnsController.TimePeriod.Monthly)));
    }

}