using System;

namespace MongoDB.Entities;

public static partial class Extensions
{
    /// <summary>
    /// converts a <see cref="DateTime"/> instance to a <see cref="Date"/> instance.
    /// </summary>
    /// <param name="dateTime">the <see cref="DateTime"/> instance to convert</param>
    public static Date ToDate(this DateTime dateTime)
        => new(dateTime);

    /// <summary>
    /// converts ticks to a <see cref="Date"/> instance.
    /// </summary>
    /// <param name="ticks">the ticks to convert</param>
    public static Date ToDate(this long ticks)
        => new(ticks);
}