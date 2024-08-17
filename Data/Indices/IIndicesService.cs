namespace Data.SyntheticIndex;

public interface ISyntheticIndexService
{
    HashSet<string> GetIndexBackfillTickers(string indexTicker);

    HashSet<string> GetAllIndexBackfillTickers(bool filterSynthetic = false);

    HashSet<string> GetIndexTickers();
}