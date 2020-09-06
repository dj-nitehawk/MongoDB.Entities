namespace MongoDB.Entities.Tests.Models
{
    [Name("Pictures")]
    public class Image : FileEntity
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string Name { get; set; }
    }
}
