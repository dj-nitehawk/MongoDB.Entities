namespace MongoDB.Entities.Tests.Models;

public class CarModel : ObjectIdEntity
{
    public string Name { get; set; }

    public string FullName { get; set; }

    public One<CarMake> Make { get; set; }

    [OwnerSide]
    public Many<CarColor, CarModel> Colors { get; set; }

    public CarModel() => this.InitManyToMany(() => Colors, g => g.Models);
}