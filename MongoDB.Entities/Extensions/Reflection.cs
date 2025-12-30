using System;
using System.Linq.Expressions;
using System.Reflection;

namespace MongoDB.Entities;

public static partial class Extensions
{
    internal static PropertyInfo PropertyInfo<T>(this Expression<T> expression)
        => (PropertyInfo)expression.MemberInfo();

    internal static MemberInfo MemberInfo<T>(this Expression<T> expression)
        => expression.Body switch
        {
            MemberExpression m => m.Member,
            UnaryExpression { Operand: MemberExpression m } => m.Member,
            _ => throw new NotSupportedException($"[{expression}] is not a valid member expression!")
        };
}