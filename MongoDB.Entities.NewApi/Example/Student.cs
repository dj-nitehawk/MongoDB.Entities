namespace MongoDB.Entities.Example;

using MongoDB.Bson;
using MongoDB.Entities.Configuration;
using MongoDB.Entities.NewApi;
using System;
using System.Collections.Generic;
using System.Text;

public class Student : IEntity<ObjectId>
{
    public ObjectId Id { get; }

    public string Name { get; set; }

    public MongoDBCollectionReference<StudentCourse> Courses { get; set; } = new();
}
