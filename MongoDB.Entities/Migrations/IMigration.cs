using System.Threading.Tasks;

namespace MongoDB.Entities
{
    /// <summary>
    /// The contract for writing user data migration classes
    /// </summary>
    public interface IMigration
    {
        Task UpgradeAsync();
    }
}
