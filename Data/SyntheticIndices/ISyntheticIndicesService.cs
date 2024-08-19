namespace Data.SyntheticIndices;

public interface ISyntheticIndicesService
{
    HashSet<string> GetSyntheticIndexTickers();

    HashSet<string> GetSyntheticIndexBackfillTickers(string syntheticIndexTicker, bool filterSynthetic = true);
}