using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MongoDB.Entities.Tests;

[TestClass]
public class ReplaceEntity
{
    [TestMethod]
    public async Task correct_doc_is_replaced()
    {
        var book = new BookEntity { Title = "book title" };
        await book.SaveAsync();

        book.Title = "updated title";

        var db = DB.Default;
        
        await db.Replace<BookEntity>()
                        .MatchID(book.ID)
                        .Match(b => b.Title == "book title")
                        .WithEntity(book)
                        .ExecuteAsync();

        var res = await db.Find<BookEntity>().OneAsync(book.ID);

        Assert.AreEqual(book.Title, res!.Title);
    }

    [TestMethod]
    public async Task correct_docs_replaced_with_bulk_replace()
    {
        var book1 = new BookEntity { Title = "book one" };
        var book2 = new BookEntity { Title = "book two" };
        var books = new[] { book1, book2 };
        await books.SaveAsync();

        var db = DB.Default;
        
        var cmd = db.Replace<BookEntity>();

        foreach (var book in books)
        {
            book.Title = book.ID;
            cmd.Match(b => b.ID == book.ID)
               .WithEntity(book)
               .AddToQueue();
        }

        await cmd.ExecuteAsync();

        var res1 = await db.Find<BookEntity>().OneAsync(book1.ID);
        var res2 = await db.Find<BookEntity>().OneAsync(book2.ID);

        Assert.AreEqual(book1.ID, res1!.Title);
        Assert.AreEqual(book2.ID, res2!.Title);
    }

    [TestMethod]
    public async Task on_before_update_for_replace()
    {
        var db = new MyDBEntity();

        var flower = new FlowerEntity { Name = "flower" };
        await db.SaveAsync(flower);
        Assert.AreEqual("God", flower.CreatedBy);

        await db.Replace<FlowerEntity>()
                .MatchID(flower.Id)
                .WithEntity(flower)
                .ExecuteAsync();

        Assert.AreEqual("Human", flower.UpdatedBy);

        var res = await db.Find<FlowerEntity>().OneAsync(flower.Id);

        Assert.AreEqual("Human", res!.UpdatedBy);
    }
}