using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Entities.Tests.Models;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests;

[TestClass]
public class ReplaceGuid
{
    [TestMethod]
    public async Task correct_doc_is_replaced()
    {
        var book = new BookGuid { Title = "book title" };
        await book.SaveAsync();

        book.Title = "updated title";

        await DB.Replace<BookGuid>()
            .MatchID(book.ID)
            .Match(b => b.Title == "book title")
            .WithEntity(book)
            .ExecuteAsync();

        var res = await DB.Find<BookGuid>().OneAsync(book.ID);

        Assert.AreEqual(book.Title, res!.Title);
    }

    [TestMethod]
    public async Task correct_docs_replaced_with_bulk_replace()
    {
        var book1 = new BookGuid { Title = "book one" };
        var book2 = new BookGuid { Title = "book two" };
        var books = new[] { book1, book2 };
        await books.SaveAsync();

        var cmd = DB.Replace<BookGuid>();

        foreach (var book in books)
        {
            book.Title = book.ID!;
            cmd.Match(b => b.ID == book.ID)
               .WithEntity(book)
               .AddToQueue();
        }

        await cmd.ExecuteAsync();

        var res1 = await DB.Find<BookGuid>().OneAsync(book1.ID);
        var res2 = await DB.Find<BookGuid>().OneAsync(book2.ID);

        Assert.AreEqual(book1.ID, res1!.Title);
        Assert.AreEqual(book2.ID, res2!.Title);
    }

    [TestMethod]
    public async Task on_before_update_for_replace()
    {
        var db = new MyDBGuid();

        var flower = new FlowerGuid { Name = "flower" };
        await db.SaveAsync(flower);
        Assert.AreEqual("God", flower.CreatedBy);

        await db.Replace<FlowerGuid>()
                .MatchID(flower.Id)
                .WithEntity(flower)
                .ExecuteAsync();

        Assert.AreEqual("Human", flower.UpdatedBy);

        var res = await db.Find<FlowerGuid>().OneAsync(flower.Id);

        Assert.AreEqual("Human", res!.UpdatedBy);
    }
}
