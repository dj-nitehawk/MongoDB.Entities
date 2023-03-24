namespace MongoDB.Entities.Tests.Models;

public class CarMake : ObjectIdEntity
{
  public string Name { get; set; }
  
  public string FullName { get; set; }
  
  public One<CarModel> BestSeller { get; set; }

  public Many<CarModel, CarMake> Models { get; set; }

  public CarMake() => this.InitOneToMany(() => Models!);
  
}