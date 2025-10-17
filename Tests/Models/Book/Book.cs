using System;
using MongoDB.Entities.Tests.Models;

namespace MongoDB.Entities.Tests;

public abstract class Book : IEntity, IModifiedOn
{
    public Date? PublishedOn { get; set; }

    [DontPreserve] public string Title { get; set; }
    [DontPreserve] public decimal Price { get; set; }

    public int PriceInt { get; set; }
    public long PriceLong { get; set; }
    public double PriceDbl { get; set; }
    public float PriceFloat { get; set; }
    public string[] Tags { get; set; }
    public One<CustomerWithCustomID> Customer { get; set; }
    public DateTime ModifiedOn { get; set; }
    public UpdatedBy ModifiedBy { get; set; }

    [Ignore]
    public int DontSaveThis { get; set; }

    public abstract object GenerateNewID();
    public abstract bool HasDefaultID();
}

public class UpdatedBy : ModifiedBy
{
    public string UserType { get; set; }
}
