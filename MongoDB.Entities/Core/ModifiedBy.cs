namespace MongoDB.Entities;

public class ModifiedBy
{
    [AsObjectId]
    public string UserID { get; set; } = null!;

    public string? UserName { get; set; }
}