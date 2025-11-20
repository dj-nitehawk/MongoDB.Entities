namespace MongoDB.Entities.Tests.Models;

[Collection("Pictures")]
public class Image : FileEntity<Image>
{
    public int Width { get; set; }
    public int Height { get; set; }
    public string Name { get; set; }
}
