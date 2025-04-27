using System.Linq.Expressions;
using System.Reflection;

namespace SpaceWarDiscordApp;

internal static  class ExpressionExtensions
{
    public static PropertyInfo GetPropertyInfo<TSource, TProperty>(
        this Expression<Func<TSource, TProperty>> propertyLambda)
    {
        Type type = typeof(TSource);

        MemberExpression member = propertyLambda.Body as MemberExpression ?? throw new ArgumentException(string.Format(
            "Expression '{0}' refers to a method, not a property.",
            propertyLambda.ToString()));

        PropertyInfo propInfo = member.Member as PropertyInfo ?? throw new ArgumentException(string.Format(
            "Expression '{0}' refers to a field, not a property.",
            propertyLambda.ToString()));

        return type != propInfo.ReflectedType && !type.IsSubclassOf(propInfo.ReflectedType!)
            ? throw new ArgumentException(string.Format(
                "Expression '{0}' refers to a property that is not from type {1}.",
                propertyLambda.ToString(),
                type))
            : propInfo;
    }
}