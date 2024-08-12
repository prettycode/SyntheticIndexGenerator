namespace Data.Indices
{
    public interface IIndicesService
    {
        Task PutSyntheticIndicesInReturnsRepository();

        HashSet<string> GetIndexBackfillTickers(bool filterSynthetic = true);
    }
}