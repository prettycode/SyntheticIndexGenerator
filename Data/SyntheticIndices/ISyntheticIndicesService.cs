namespace Data.SyntheticIndices;

public interface ISyntheticIndicesService
{
    Task PutSyntheticIndicesInReturnsRepository();

    [Obsolete]
    HashSet<string> GetIndexBackfillTickers(bool filterSynthetic = true);

    HashSet<string> GetSyntheticIndexTickers();

    HashSet<string> GetSyntheticIndexBackfillTickers(string syntheticIndexTicker, bool filterSynthetic = true);
}