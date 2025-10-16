using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests;

[TestClass]
public class ReplaceInt64
{
    [TestMethod]
    public async Task correct_doc_is_replaced()
    {
        var book = new BookInt64 { Title = "book title" };
        await book.SaveAsync();

        book.Title = "updated title";

        await DBInstance.Instance().Replace<BookInt64>()
            .MatchID(book.ID)
            .Match(b => b.Title == "book title")
            .WithEntity(book)
            .ExecuteAsync();

        var res = await DBInstance.Instance().Find<BookInt64>().OneAsync(book.ID);

        Assert.AreEqual(book.Title, res!.Title);
    }

    [TestMethod]
    public async Task correct_docs_replaced_with_bulk_replace()
    {
        var book1 = new BookInt64 { Title = "book one" };
        var book2 = new BookInt64 { Title = "book two" };
        var books = new[] { book1, book2 };
        await books.SaveAsync();

        var cmd = DBInstance.Instance().Replace<BookInt64>();

        foreach (var book in books)
        {
            book.Title = book.ID.ToString();
            cmd.Match(b => b.ID == book.ID)
               .WithEntity(book)
               .AddToQueue();
        }

        await cmd.ExecuteAsync();

        var res1 = await DBInstance.Instance().Find<BookInt64>().OneAsync(book1.ID);
        var res2 = await DBInstance.Instance().Find<BookInt64>().OneAsync(book2.ID);

        Assert.AreEqual(book1.ID.ToString(), res1!.Title);
        Assert.AreEqual(book2.ID.ToString(), res2!.Title);
    }

    [TestMethod]
    public async Task on_before_update_for_replace()
    {
        var db = new MyDBInt64();

        var flower = new FlowerInt64 { Name = "flower" };
        await db.SaveAsync(flower);
        Assert.AreEqual("God", flower.CreatedBy);

        await db.Replace<FlowerInt64>()
                .MatchID(flower.Id)
                .WithEntity(flower)
                .ExecuteAsync();

        Assert.AreEqual("Human", flower.UpdatedBy);

        var res = await db.Find<FlowerInt64>().OneAsync(flower.Id);

        Assert.AreEqual("Human", res!.UpdatedBy);
    }
}
