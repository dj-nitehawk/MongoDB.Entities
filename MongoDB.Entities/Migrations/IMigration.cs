namespace MongoDB.Entities
{
    public interface IMigration
    {
        void Upgrade();
    }
}
