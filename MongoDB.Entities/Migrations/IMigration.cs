using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public interface IMigration
    {
        Task UpgradeAsync();
    }
}
