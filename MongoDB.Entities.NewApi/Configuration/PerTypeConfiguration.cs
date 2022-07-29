namespace MongoDB.Entities.Configuration;

internal class PerTypeConfiguration<T>
{
    public string? DefaultCollectionName { get; set; }
    public List<PerRelationConfiguration<T>> Relations { get; } = new();
}
