using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests;

[TestClass]
public class DefaultDatabaseChangingObjectId
{
    [TestMethod]
    public void throw_argument_null_exception()
    {
        Assert.ThrowsException<ArgumentNullException>(() =>  DBInstance.ChangeDefaultDatabase(""));
    }

    [TestMethod]
    public void throw_invalid_operation_exception()
    {
        Assert.ThrowsException<InvalidOperationException>(() =>  DBInstance.ChangeDefaultDatabase("db3"));
    }

    [TestMethod]
    public async Task returns_correct_database()
    {
        await  DBInstance.InitAsync("test1");
        await  DBInstance.InitAsync("test2");

        var defaultDb = DBInstance.Instance().Database();
        var database = DBInstance.Instance("test2").Database();

         DBInstance.ChangeDefaultDatabase("test2");

        var bookDb = DBInstance.Instance().Database<BookObjectId>();

        Assert.AreEqual(database.DatabaseNamespace.DatabaseName, bookDb.DatabaseNamespace.DatabaseName);

         DBInstance.ChangeDefaultDatabase(defaultDb.DatabaseNamespace.DatabaseName);
    }

    [TestMethod]
    public async Task do_not_change_default_database_when_the_same()
    {
        await  DBInstance.InitAsync("test1");

        var defaultDb = DBInstance.Instance().Database();
        var defaultDbName = DBInstance.Instance().DatabaseName<AuthorObjectId>();

         DBInstance.ChangeDefaultDatabase(defaultDbName);

        var bookDb = DBInstance.Instance().Database<BookObjectId>();
        Assert.AreSame(defaultDb, bookDb);
    }
}