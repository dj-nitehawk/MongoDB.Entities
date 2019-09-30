using System;
using System.Linq.Expressions;

namespace MongoDB.Entities
{
    //todo: xml docs
    public static class Prop
    {
        //MoreReviews[0].Rating > MoreReviews.Rating
        public static string Dotted<T>(Expression<Func<T, object>> expression)
        {
            if (expression == null) return null;
            var name = expression.Parameters[0].Name;
            return expression.ToString()
                       .Replace($"{name} => {name}.", "")
                       .Replace($"{name} => Convert({name}.", "")
                       .Replace(", Object)", "")
                       .Replace("get_Item(0).", "")
                       .Replace("[0]", "");
        }

        //MoreReviews[1].Rating > MoreReviews.$[1].Rating
        public static string PosFiltered<T>(Expression<Func<T, object>> expression)
        {
            if (expression == null) return null;
            var name = expression.Parameters[0].Name;
            return expression.ToString()
                       .Replace($"{name} => {name}.", "")
                       .Replace($"{name} => Convert({name}.", "")
                       .Replace(", Object)", "")
                       .Replace("[", ".$[")
                       .Replace("get_Item(", "$[").Replace(")", "]");
        }

        //MoreReviews[0].Rating > MoreReviews.$[].Rating
        public static string PosAll<T>(Expression<Func<T, object>> expression)
        {
            if (expression == null) return null;
            var name = expression.Parameters[0].Name;
            return expression.ToString()
                       .Replace($"{name} => {name}.", "")
                       .Replace($"{name} => Convert({name}.", "")
                       .Replace(", Object)", "")
                       .Replace("[0]", ".$[]")
                       .Replace("get_Item(0)", "$[]");
        }

        //MoreReviews[0].Rating > MoreReviews.$.Rating
        public static string Pos<T>(Expression<Func<T, object>> expression)
        {
            if (expression == null) return null;
            var name = expression.Parameters[0].Name;
            return expression.ToString()
                       .Replace($"{name} => {name}.", "")
                       .Replace($"{name} => Convert({name}.", "")
                       .Replace(", Object)", "")
                       .Replace("get_Item(0)", "$")
                       .Replace("[0]", ".$");
        }
    }
}
