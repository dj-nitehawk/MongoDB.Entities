namespace MongoDB.Entities.Tests.Models;

public class Auto : Entity
{
    public string Make { get; set; }
    public string Model { get; set; }
    public int Year { get; set; }
}