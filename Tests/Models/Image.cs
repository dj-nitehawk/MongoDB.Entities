namespace MongoDB.Entities.Tests.Models
{
    [Database("mongodb-entities-test-multi")]
    [Name("Pictures")]
    public class Image : FileEntity
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string Name { get; set; }
    }
}
