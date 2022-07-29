namespace MongoDB.Entities.Example;

using MongoDB.Bson;
using MongoDB.Entities.Configuration;
using MongoDB.Entities.NewApi;

public class Teacher : IEntity<ObjectId>
{
    public ObjectId Id { get; }

    public string Name { get; set; }

    public MongoDBCollectionReference<Course> CoursesAsMainTeacher { get; set; } = new();
    public MongoDBCollectionReference<Course> CoursesAsSubstituteTeacher { get; set; } = new();
}