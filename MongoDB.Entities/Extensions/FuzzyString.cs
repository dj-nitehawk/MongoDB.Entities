namespace MongoDB.Entities;

public static partial class Extensions
{
    /// <summary>
    /// converts a string value to a FuzzyString
    /// </summary>
    /// <param name="value">the string to convert</param>
    public static FuzzyString ToFuzzy(this string value)
        => new(value);
}