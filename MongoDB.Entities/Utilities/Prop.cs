using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace MongoDB.Entities
{
    //todo: xml docs
    public static class Prop
    {
        private static string ToLowerLetter(long number)
        {
            string returnVal = null;
            char c = 'a';
            while (number >= 0)
            {
                returnVal = (char)(c + number % 26) + returnVal;
                number /= 26;
                number--;
            }

            return returnVal;
        }

        //Authors[0].Name > Authors.Name
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

        //Authors[0].Name > Authors.$[a].Name
        //Authors[1].Name > Authors.$[b].Name
        public static string PosFiltered<T>(Expression<Func<T, object>> expression)
        {
            if (expression == null) return null;
            var name = expression.Parameters[0].Name;
            var path = expression.ToString()
                       .Replace($"{name} => {name}.", "")
                       .Replace($"{name} => Convert({name}.", "")
                       .Replace(", Object)", "")
                       .Replace("[", ".$[")
                       .Replace("get_Item(", "$[").Replace(")", "]");

            return Regex.Replace(path, @"(?<=\[).+?(?=\])", m => ToLowerLetter(int.Parse(m.Value)));
        }

        //Authors[0].Name > Authors.$[].Name
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

        //Authors[0].Name > Authors.$.Name
        public static string PosFirst<T>(Expression<Func<T, object>> expression)
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

        // book => book.Tags > Tags
        public static string Elements<T>(Expression<Func<T, object>> expression)
        {
            return Dotted(expression);
        }

        // 0 | book => book.Rating > a.Rating
        // 1 | book => book.Rating > b.Rating
        public static string Elements<T>(int index, Expression<Func<T, object>> expression)
        {
            return $"{ToLowerLetter(index)}.{Dotted(expression)}";
        }
    }
}
