namespace MongoDB.Entities;

/// <summary>
/// Represents a migration history item in the database
/// </summary>
[Collection("_migration_history_")]
public class Migration(int number, string name, double timeTakenSeconds) : Entity
{
    public int Number { get; set; } = number;
    public string Name { get; set; } = name;
    public double TimeTakenSeconds { get; set; } = timeTakenSeconds;
}