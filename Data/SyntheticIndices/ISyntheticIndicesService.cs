namespace Data.SyntheticIndices;

public interface ISyntheticIndexService
{
    HashSet<string> GetIndexBackfillTickers(string indexTicker, bool filterSynthetic = true);

    HashSet<string> GetAllIndexBackfillTickers(bool filterSynthetic = true);

    HashSet<string> GetIndexTickers();
}