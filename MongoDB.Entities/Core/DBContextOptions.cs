#nullable enable

using MongoDB.Driver;

namespace MongoDB.Entities
{
    public class DBContextOptions
    {
        public DBContextOptions(string? tenantId = null)
        {
            TenantId = tenantId;
        }

        public string? TenantId { get; set; }
        
    }
}
