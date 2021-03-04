namespace MongoDB.Entities
{
    public class ModifiedBy
    {
        [AsObjectId] public string UserID { get; set; }
        public string UserName { get; set; }
    }
}
