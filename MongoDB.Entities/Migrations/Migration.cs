namespace MongoDB.Entities;

/// <summary>
/// Represents a migration history item in the database
/// </summary>
[Collection("_migration_history_")]
public class Migration : Entity
{
    public int Number { get; set; }
    public string Name { get; set; }
    public double TimeTakenSeconds { get; set; }

    public Migration(int number, string name, double timeTakenSeconds)
    {
        Number = number;
        Name = name;
        TimeTakenSeconds = timeTakenSeconds;

    }
}
