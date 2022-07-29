namespace MongoDB.Entities.Example;

using MongoDB.Bson;
using MongoDB.Entities.Configuration;
using MongoDB.Entities.NewApi;

public class Course : IEntity<ObjectId>
{
    public ObjectId Id { get; }
    public string Name { get; set; }


    /// <summary>
    /// This API shouldn't assume the Id structre of the related entities
    /// </summary>
    public ObjectId TeacherId { get; set; }
    public MongoDBDocumentReference<Teacher> Teacher { get; set; } = new();
    //Also works:
    //public DocumentReference<BsonDocument> Teacher { get; set; } = new();

    public ObjectId? SubstituteTeacherId { get; set; }
    public MongoDBDocumentReference<Teacher> SubstituteTeacher { get; set; } = new();

    public MongoDBCollectionReference<StudentCourse> Participants { get; set; } = new();
}
