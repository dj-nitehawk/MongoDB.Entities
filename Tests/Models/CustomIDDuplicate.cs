namespace MongoDB.Entities.Tests.Models;

public class CustomIDDuplicate : Entity
{
    public override string GenerateNewID()
        => "iamnotauniqueid";
}
