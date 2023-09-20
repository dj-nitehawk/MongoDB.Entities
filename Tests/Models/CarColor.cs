namespace MongoDB.Entities.Tests.Models;

public class CarColor : ObjectIdEntity
{
    public string Name { get; set; }

    [InverseSide]
    public Many<CarModel, CarColor> Models { get; set; }

    public CarColor() => this.InitManyToMany(() => Models, g => g.Colors);

}