
namespace Data.Controllers
{
    public interface IIndicesService
    {
        Task RefreshIndices();

        HashSet<string> GetRequiredTickers(bool filterSynthetic = true);
    }
}