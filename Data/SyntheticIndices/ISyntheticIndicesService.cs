namespace Data.SyntheticIndices;

public interface ISyntheticIndicesService
{
    Task PutSyntheticIndicesInReturnsRepository();

    HashSet<string> GetIndexBackfillTickers(bool filterSynthetic = true);
}