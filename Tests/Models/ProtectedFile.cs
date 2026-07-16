namespace MongoDB.Entities.Tests.Models;

[Collection("ProtectedFile")]
public class ProtectedFile : FileEntity<ProtectedFile>
{
    public string Tenant { get; set; } = null!;
    public string Name { get; set; } = null!;
}
