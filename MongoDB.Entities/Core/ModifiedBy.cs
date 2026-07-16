namespace MongoDB.Entities;

public class ModifiedBy
{
    public string UserID { get; set; } = null!;

    public string? UserName { get; set; }
}