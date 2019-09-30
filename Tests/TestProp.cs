using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq.Expressions;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class Props
    {
        [TestMethod]
        public void prop_dotted()
        {
            Expression<Func<Book, object>> exp = x => x.ReviewArray[0].Rating;
            var res = exp.FullPath();
            Assert.AreEqual("ReviewArray.Rating", res);

            Expression<Func<Book, object>> exp1 = x => x.ReviewArray[0].Books[0].ReviewArray[0].Books[0].ModifiedOn;
            var res1 = exp1.FullPath();
            Assert.AreEqual("ReviewArray.Books.ReviewArray.Books.ModifiedOn", res1);
        }

        [TestMethod]
        public void prop_pos_filtered()
        {
            var res1 = Prop.PosFiltered<Book>(b => b.ReviewArray[0].Books[1].MainAuthor.ID);
            Assert.AreEqual("ReviewArray.$[a].Books.$[b].MainAuthor.ID", res1);

            var res2 = Prop.PosFiltered<Book>(b => b.ReviewList[0].Rating);
            Assert.AreEqual("ReviewList.$[a].Rating", res2);
        }

        [TestMethod]
        public void prop_pos_all()
        {
            var res1 = Prop.PosAll<Book>(b => b.ReviewArray[0].Rating);
            Assert.AreEqual("ReviewArray.$[].Rating", res1);

            var res2 = Prop.PosAll<Book>(b => b.ReviewList[0].Rating);
            Assert.AreEqual("ReviewList.$[].Rating", res2);
        }

        [TestMethod]
        public void prop_pos()
        {
            var res1 = Prop.PosFirst<Book>(b => b.ReviewArray[0].Rating);
            Assert.AreEqual("ReviewArray.$.Rating", res1);

            var res2 = Prop.PosFirst<Book>(b => b.ReviewList[0].Rating);
            Assert.AreEqual("ReviewList.$.Rating", res2);
        }

        [TestMethod]
        public void prop_elements_root()
        {
            var res = Prop.Elements<Book>(b => b.Tags);
            Assert.AreEqual("Tags", res);
        }

        [TestMethod]
        public void prop_elements_nested()
        {
            var res = Prop.Elements<Book>(0, b => b.Tags);
            Assert.AreEqual("a.Tags", res);

            var res2 = Prop.Elements<Book>(1, b => b.ReviewList[0].Rating);
            Assert.AreEqual("b.ReviewList.Rating", res2);
        }
    }
}
