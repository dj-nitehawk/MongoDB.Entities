namespace MongoDB.Entities
{
    /// <summary>
    /// The contract for Entity classes
    /// </summary>
    public interface IEntity
    {
        string ID { get; set; }
    }
}