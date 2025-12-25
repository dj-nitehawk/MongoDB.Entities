using System;
using System.Linq.Expressions;
using System.Reflection;

namespace MongoDB.Entities;

public static partial class Extensions
{
    extension<T>(Expression<T> expression)
    {
        internal PropertyInfo PropertyInfo()
            => (PropertyInfo)expression.MemberInfo();

        internal MemberInfo MemberInfo()
            => expression.Body switch
            {
                MemberExpression m => m.Member,
                UnaryExpression { Operand: MemberExpression m } => m.Member,
                _ => throw new NotSupportedException($"[{expression}] is not a valid member expression!")
            };
    }
}