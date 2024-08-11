
namespace Data.Services
{
    public interface IIndicesService
    {
        Task PutSyntheticIndicesInReturnsRepository();

        HashSet<string> GetIndexBackfillTickers(bool filterSynthetic = true);
    }
}