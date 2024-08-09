
namespace Data.Services
{
    public interface IIndicesService
    {
        Task RefreshIndices();

        HashSet<string> GetRequiredTickers(bool filterSynthetic = true);
    }
}