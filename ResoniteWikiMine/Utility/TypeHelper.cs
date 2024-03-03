namespace ResoniteWikiMine.Utility;

/// <summary>
/// Helper functions for working with reflection types.
/// </summary>
public static class TypeHelper
{
    /// <summary>
    /// Recursively descend through a type, such that
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static IEnumerable<Type> GenericTypesRecursive(Type type)
    {
        return LinqExtensions.Flatten(type, static subType =>
        {
            if (!subType.IsGenericType)
                return [];

            return subType.GetGenericArguments();
        });
    }
}
