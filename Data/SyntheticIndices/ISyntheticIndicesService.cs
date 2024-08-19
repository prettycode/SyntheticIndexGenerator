namespace Data.SyntheticIndices;

public interface ISyntheticIndicesService
{
    HashSet<string> GetIndexBackfillTickers(string indexTicker, bool filterSynthetic = true);

    HashSet<string> GetAllIndexBackfillTickers(bool filterSynthetic = true);

    HashSet<string> GetIndexTickers();
}