using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace MongoDB.Entities
{
    /// <summary>
    /// This class provides methods to generate property path strings from lambda expression. 
    /// </summary>
    public static class Prop
    {
        private static string GetLowerLetter(long number)
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

        private static string GetPath<T>(Expression<Func<T, object>> expression)
        {
            if (expression.Body.NodeType == ExpressionType.Parameter)
                throw new ArgumentException("Cannot generate property path from lambda parameter!");

            //One.Two[1].Three.get_Item(2).Four
            var path = Regex.Match(
                                expression.ToString(),
                                @"(?:\.(?:\w+(?:[[(]\d+[)\]])?))+")
                            .Value
                            .Substring(1);

            //One.Two[1].Three[2].Four
            return Regex.Replace(
                            path,
                            @".get_Item\((\d+)\)",
                            m => "[" + m.Groups[1].Value + "]");
        }

        /// <summary>
        /// Returns the full dotted path for a given expression.
        /// <para>EX: Authors[0].Books[0].Title > Authors.Books.Title</para>
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public static string Dotted<T>(Expression<Func<T, object>> expression)
        {
            if (expression == null) return null;

            return Regex.Replace(
                            GetPath(expression),
                            @"\[\d+\]",
                            "");
        }

        /// <summary>
        /// Returns a path with filtered positional identifiers $[x] for a given expression.
        /// <para>EX: Authors[0].Name > Authors.$[a].Name</para>
        /// <para>EX: Authors[1].Age > Authors.$[b].Age</para>
        /// <para>EX: Authors[2].Books[3].Title > Authors.$[c].Books.$[d].Title</para>
        /// <para>TIP: Index positions start from [0] which is converted to $[a] and so on.</para>
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public static string PosFiltered<T>(Expression<Func<T, object>> expression)
        {
            if (expression == null) return null;

            return Regex.Replace(
                            GetPath(expression),
                            @"\[(\d+)\]",
                            m => ".$[" + GetLowerLetter(int.Parse(m.Groups[1].Value)) + "]");
        }

        /// <summary>
        /// Returns a path with the all positional operator $[] for a given expression.
        /// <para>EX: Authors[0].Name > Authors.$[].Name</para>
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public static string PosAll<T>(Expression<Func<T, object>> expression)
        {
            if (expression == null) return null;

            return Regex.Replace(
                            GetPath(expression),
                            @"\[\d+\]",
                            ".$[]");
        }

        /// <summary>
        /// Returns a path with the first positional operator $ for a given expression.
        /// <para>EX: Authors[0].Name > Authors.$.Name</para>
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public static string PosFirst<T>(Expression<Func<T, object>> expression)
        {
            if (expression == null) return null;

            return Regex.Replace(
                            GetPath(expression),
                            @"\[\d+\]",
                            ".$");
        }

        /// <summary>
        /// Returns a path without any filtered positional identifier prepended to it.
        /// <para>EX: b => b.Tags > Tags</para>
        /// </summary>
        /// <param name="expression">x => x.SomeProp</param>
        public static string Elements<T>(Expression<Func<T, object>> expression)
        {
            return Dotted(expression);
        }

        /// <summary>
        /// Returns a path with the filtered positional identifier prepended to the property path.
        /// <para>EX: 0, x => x.Rating > a.Rating</para>
        /// <para>EX: 1, x => x.Rating > b.Rating</para>
        /// <para>TIP: Index positions start from '0' which is converted to 'a' and so on.</para>
        /// </summary>
        /// <param name="index">0=a 1=b 2=c 3=d and so on...</param>
        /// <param name="expression">x => x.SomeProp</param>
        public static string Elements<T>(int index, Expression<Func<T, object>> expression)
        {
            return $"{GetLowerLetter(index)}.{Dotted(expression)}";
        }
    }
}
