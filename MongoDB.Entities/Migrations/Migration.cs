namespace MongoDB.Entities
{
    /// <summary>
    /// Represents a migration history item in the database
    /// </summary>
    [Name("_migration_history_")]
    public class Migration : Entity
    {
        public int Number { get; set; }
        public string Name { get; set; }
        public double TimeTakenSeconds { get; set; }
    }
}
