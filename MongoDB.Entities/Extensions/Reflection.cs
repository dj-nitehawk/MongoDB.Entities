using System;
using System.Linq.Expressions;
using System.Reflection;

namespace MongoDB.Entities
{
    public static partial class Extensions
    {
        internal static PropertyInfo PropertyInfo<T>(this Expression<T> expression)
        {
            return MemberInfo(expression) as PropertyInfo;
        }

        internal static MemberInfo MemberInfo<T>(this Expression<T> expression)
        {
            switch (expression.Body)
            {
                case MemberExpression m:
                    return m.Member;
                case UnaryExpression u when u.Operand is MemberExpression m:
                    return m.Member;
                default:
                    throw new NotSupportedException($"[{expression}] is not a valid member expression!");
            }
        }
    }
}
