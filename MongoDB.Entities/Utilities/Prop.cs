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
        private static readonly Regex rxOne = new Regex(@"(?:\.(?:\w+(?:[[(]\d+[)\]])?))+", RegexOptions.Compiled);
        private static readonly Regex rxTwo = new Regex(@".get_Item\((\d+)\)", RegexOptions.Compiled);
        private static readonly Regex rxThree = new Regex(@"\[\d+\]", RegexOptions.Compiled);
        private static readonly Regex rxFour = new Regex(@"\[(\d+)\]", RegexOptions.Compiled);

        private static string ToLowerCaseLetter(long number)
        {
            string returnVal = null;
            const char c = 'a';
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
            if (expression == null)
                throw new ArgumentNullException(nameof(expression), "The supplied expression is null!");

            if (expression.Body.NodeType == ExpressionType.Parameter)
                throw new ArgumentException("Cannot generate property path from lambda parameter!");

            //One.Two[1].Three.get_Item(2).Four
            var path = rxOne.Match(expression.ToString()).Value.Substring(1);

            //One.Two[1].Three[2].Four
            return rxTwo.Replace(path, m => "[" + m.Groups[1].Value + "]");
        }

        /// <summary>
        /// Returns the full dotted path for a given expression.
        /// <para>EX: Authors[0].Books[0].Title > Authors.Books.Title</para>
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public static string Path<T>(Expression<Func<T, object>> expression)
        {
            return rxThree.Replace(GetPath(expression), "");
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
            return rxFour.Replace(
                            GetPath(expression),
                            m => ".$[" + ToLowerCaseLetter(int.Parse(m.Groups[1].Value)) + "]");
        }

        /// <summary>
        /// Returns a path with the all positional operator $[] for a given expression.
        /// <para>EX: Authors[0].Name > Authors.$[].Name</para>
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public static string PosAll<T>(Expression<Func<T, object>> expression)
        {
            return rxThree.Replace(GetPath(expression), ".$[]");
        }

        /// <summary>
        /// Returns a path with the first positional operator $ for a given expression.
        /// <para>EX: Authors[0].Name > Authors.$.Name</para>
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public static string PosFirst<T>(Expression<Func<T, object>> expression)
        {
            return rxThree.Replace(GetPath(expression), ".$");
        }

        /// <summary>
        /// Returns a path without any filtered positional identifier prepended to it.
        /// <para>EX: b => b.Tags > Tags</para>
        /// </summary>
        /// <param name="expression">x => x.SomeProp</param>
        public static string Elements<T>(Expression<Func<T, object>> expression)
        {
            return Path(expression);
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
            return $"{ToLowerCaseLetter(index)}.{Path(expression)}";
        }
    }
}
