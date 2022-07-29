namespace MongoDB.Entities.Example;

using MongoDB.Bson;
using MongoDB.Entities.Configuration;

public class StudentCourse
{
    public ObjectId StudentId { get; set; }
    public MongoDBDocumentReference<Student> Student { get; set; } = new();

    public ObjectId CourseId { get; set; }
    public MongoDBDocumentReference<Course> Course { get; set; } = new();

    public bool Passed { get; set; }
}