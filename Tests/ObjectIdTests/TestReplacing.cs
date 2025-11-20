using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MongoDB.Entities.Tests;

[TestClass]
public class ReplaceObjectId
{
    [TestMethod]
    public async Task correct_doc_is_replaced()
    {
        var book = new BookObjectId { Title = "book title" };
        await book.SaveAsync();

        book.Title = "updated title";

        await DB.Instance().Replace<BookObjectId>()
            .MatchID(book.ID)
            .Match(b => b.Title == "book title")
            .WithEntity(book)
            .ExecuteAsync();

        var res = await DB.Instance().Find<BookObjectId>().OneAsync(book.ID);

        Assert.AreEqual(book.Title, res!.Title);
    }

    [TestMethod]
    public async Task correct_docs_replaced_with_bulk_replace()
    {
        var book1 = new BookObjectId { Title = "book one" };
        var book2 = new BookObjectId { Title = "book two" };
        var books = new[] { book1, book2 };
        await books.SaveAsync();

        var cmd = DB.Instance().Replace<BookObjectId>();

        foreach (var book in books)
        {
            book.Title = book.ID.ToString()!;
            cmd.Match(b => b.ID == book.ID)
               .WithEntity(book)
               .AddToQueue();
        }

        await cmd.ExecuteAsync();

        var res1 = await DB.Instance().Find<BookObjectId>().OneAsync(book1.ID);
        var res2 = await DB.Instance().Find<BookObjectId>().OneAsync(book2.ID);

        Assert.AreEqual(book1.ID.ToString(), res1!.Title);
        Assert.AreEqual(book2.ID.ToString(), res2!.Title);
    }

    [TestMethod]
    public async Task on_before_update_for_replace()
    {
        var db = new MyDBObjectId();

        var flower = new FlowerObjectId { Name = "flower" };
        await db.SaveAsync(flower);
        Assert.AreEqual("God", flower.CreatedBy);

        await db.Replace<FlowerObjectId>()
                .MatchID(flower.Id)
                .WithEntity(flower)
                .ExecuteAsync();

        Assert.AreEqual("Human", flower.UpdatedBy);

        var res = await db.Find<FlowerObjectId>().OneAsync(flower.Id);

        Assert.AreEqual("Human", res!.UpdatedBy);
    }
}
