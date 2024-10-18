using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace MongoDB.Entities;

/// <summary>
/// This class provides methods to generate property path strings from lambda expression.
/// </summary>
public static class Prop
{
    static readonly Regex _rxOne = new(@"(?:\.(?:\w+(?:[[(]\d+[)\]])?))+", RegexOptions.Compiled); //matched result: One.Two[1].Three.get_Item(2).Four
    static readonly Regex _rxTwo = new(@".get_Item\((\d+)\)", RegexOptions.Compiled);              //replaced result: One.Two[1].Three[2].Four
    static readonly Regex _rxThree = new(@"\[\d+\]", RegexOptions.Compiled);
    static readonly Regex _rxFour = new(@"\[(\d+)\]", RegexOptions.Compiled);

    static string ToLowerCaseLetter(long n)
    {
        if (n < 0)
            throw new NotSupportedException("Value must be greater than 0!");

        string? val = null;
        const char c = 'a';

        while (n >= 0)
        {
            val = (char)(c + n % 26) + val;
            n /= 26;
            n--;
        }

        return val!;
    }

    static void ThrowIfInvalid<T>(Expression<Func<T, object?>> expression)
    {
        if (expression == null)
            throw new ArgumentNullException(nameof(expression), "The supplied expression is null!");

        if (expression.Body.NodeType == ExpressionType.Parameter)
            throw new ArgumentException("Cannot generate property path from lambda parameter!");
    }

    static string GetPath<T>(Expression<Func<T, object?>> expression)
    {
        ThrowIfInvalid(expression);

        return _rxTwo.Replace(
            _rxOne.Match(expression.ToString()).Value[1..],
            m => "[" + m.Groups[1].Value + "]");
    }

    internal static string GetPath(string expString)
    {
        return
            _rxThree.Replace(
                _rxTwo.Replace(
                    _rxOne.Match(expString).Value[1..],
                    m => "[" + m.Groups[1].Value + "]"),
                "");
    }

    /// <summary>
    /// Returns the collection/entity name of a given entity type
    /// </summary>
    /// <typeparam name="T">The type of the entity to get the collection name of</typeparam>
    public static string Collection<T>() where T : IEntity
        => Cache<T>.CollectionName;

    /// <summary>
    /// Returns the name of the property for a given expression.
    /// <para>EX: Authors[0].Books[0].Title > Title</para>
    /// </summary>
    /// <param name="expression">x => x.SomeList[0].SomeProp</param>
    public static string Property<T>(Expression<Func<T, object?>> expression)
    {
        ThrowIfInvalid(expression);

        return expression.MemberInfo().Name;
    }

    /// <summary>
    /// Returns the full dotted path for a given expression.
    /// <para>EX: Authors[0].Books[0].Title > Authors.Books.Title</para>
    /// </summary>
    /// <param name="expression">x => x.SomeList[0].SomeProp</param>
    public static string Path<T>(Expression<Func<T, object?>> expression)
        => _rxThree.Replace(GetPath(expression), "");

    /// <summary>
    /// Returns a path with filtered positional identifiers $[x] for a given expression.
    /// <para>EX: Authors[0].Name > Authors.$[a].Name</para>
    /// <para>EX: Authors[1].Age > Authors.$[b].Age</para>
    /// <para>EX: Authors[2].Books[3].Title > Authors.$[c].Books.$[d].Title</para>
    /// <para>TIP: Index positions start from [0] which is converted to $[a] and so on.</para>
    /// </summary>
    /// <param name="expression">x => x.SomeList[0].SomeProp</param>
    public static string PosFiltered<T>(Expression<Func<T, object?>> expression)
    {
        return _rxFour.Replace(
            GetPath(expression),
            m => ".$[" + ToLowerCaseLetter(int.Parse(m.Groups[1].Value)) + "]");
    }

    /// <summary>
    /// Returns a path with the all positional operator $[] for a given expression.
    /// <para>EX: Authors[0].Name > Authors.$[].Name</para>
    /// </summary>
    /// <param name="expression">x => x.SomeList[0].SomeProp</param>
    public static string PosAll<T>(Expression<Func<T, object?>> expression)
        => _rxThree.Replace(GetPath(expression), ".$[]");

    /// <summary>
    /// Returns a path with the first positional operator $ for a given expression.
    /// <para>EX: Authors[0].Name > Authors.$.Name</para>
    /// </summary>
    /// <param name="expression">x => x.SomeList[0].SomeProp</param>
    public static string PosFirst<T>(Expression<Func<T, object?>> expression)
        => _rxThree.Replace(GetPath(expression), ".$");

    /// <summary>
    /// Returns a path without any filtered positional identifier prepended to it.
    /// <para>EX: b => b.Tags > Tags</para>
    /// </summary>
    /// <param name="expression">x => x.SomeProp</param>
    public static string Elements<T>(Expression<Func<T, object?>> expression)
        => Path(expression);

    /// <summary>
    /// Returns a path with the filtered positional identifier prepended to the property path.
    /// <para>EX: 0, x => x.Rating > a.Rating</para>
    /// <para>EX: 1, x => x.Rating > b.Rating</para>
    /// <para>TIP: Index positions start from '0' which is converted to 'a' and so on.</para>
    /// </summary>
    /// <param name="index">0=a 1=b 2=c 3=d and so on...</param>
    /// <param name="expression">x => x.SomeProp</param>
    public static string Elements<T>(int index, Expression<Func<T, object?>> expression)
        => $"{ToLowerCaseLetter(index)}.{Path(expression)}";
}