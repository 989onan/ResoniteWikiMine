using System.Reflection;
using System.Text;
using Elements.Core;
using FrooxEngine;

namespace ResoniteWikiMine.Generation;

/// <summary>
/// Helper functions for formatting C# field types to wiki text.
/// </summary>
public static class FieldFormatter
{
    // Fields that should be skipped.
    private static readonly string[] SkipComponentFields = ["Enabled", "UpdateOrder", "persistent"];

    // These "wrapping" field types are "invisible" with Template:RootFieldType.
    // This means we can directly have Table ComponentFields handle them,
    // instead of having to use an advanced type with Template:RootFieldType.
    // We could just elide this list and use Template:RootFieldType,
    // but that would bloat a ton of pages with non-complex field types,
    // and also it'd create a pretty major diff I don't wanna look over vs the initial wiki launch.
    private static readonly HashSet<Type> SilentFieldTypes =
    [
        typeof(Sync<>),
        typeof(SyncRef<>)
    ];

    private static readonly Dictionary<Type, string> SpecialTypeNames = new()
    {
        { typeof(float), "Float" },
        { typeof(bool), "Bool" },
        { typeof(int), "Int" },
        { typeof(long), "Long" },
        { typeof(ulong), "Ulong" },
        { typeof(short), "Short" },
        { typeof(ushort), "Ushort" },
    };

    private const string DefaultNamespace = "Type:";

    private static readonly (Type, string)[] TypeNamespaces =
    [
        // Materials aren't in a namespace, for some reason.
        (typeof(MaterialProvider), ""),
        (typeof(Component), "Component:"),
    ];

    internal static string MakeComponentFieldsTemplate(Type type, Dictionary<string, string>? descriptions = null)
    {
        var sb = new StringBuilder();
        sb.Append("{{Table ComponentFields\n");

        MakeFieldsTemplateCore(sb, type, SkipComponentFields, descriptions);

        sb.Append("}}");
        return sb.ToString();
    }

    internal static string MakeTypeFieldsTemplate(
        Type type,
        Dictionary<string, string>? descriptions = null,
        Type? containingType = null)
    {
        var sb = new StringBuilder();
        sb.Append("{{Table TypeFields\n");

        MakeFieldsTemplateCore(sb, type, Array.Empty<string>(), descriptions, containingType);

        sb.Append("}}");
        return sb.ToString();
    }

    private static void MakeFieldsTemplateCore(
        StringBuilder sb,
        Type type,
        string[] skipFields,
        Dictionary<string, string>? descriptions = null,
        Type? containingType = null)
    {
        var list = EnumerateFilteredSyncFields(type, skipFields).ToArray();
        for (var i = 0; i < list.Length; i++)
        {
            var field = list[i];
            var desc = GetDescription(field, descriptions);
            var (fieldType, advanced) = FormatFieldType(field.Type, containingType ?? type);

            sb.Append($"|{field.Name}");
            sb.Append($"|{fieldType}");
            if (advanced)
                sb.Append($"|TypeAdv{i}=true");
            sb.Append($"|{desc}");

            sb.Append(field.Description);
            sb.Append('\n');
        }
    }

    private static string GetDescription(TypeFieldsEntry field, Dictionary<string, string>? descriptions)
    {
        if (descriptions == null)
            return "";

        if (descriptions.TryGetValue(field.Name, out var desc) && !string.IsNullOrWhiteSpace(desc))
            return desc;

        if (field.OldNames != null)
        {
            foreach (var oldName in field.OldNames)
            {
                if (descriptions.TryGetValue(oldName, out desc) && !string.IsNullOrWhiteSpace(desc))
                    return desc;
            }
        }

        return "";
    }

    private static IEnumerable<TypeFieldsEntry> EnumerateFilteredSyncFields(Type type, string[] skipFields)
    {
        return EnumerateSyncFields(type).Where(tuple => Array.IndexOf(skipFields, tuple.Name) == -1);
    }

    public static IEnumerable<TypeFieldsEntry> EnumerateSyncFields(Type type)
    {
        var initInfo = (WorkerInitInfo)typeof(WorkerInitializer)
            .GetMethod("GetInitInfo", BindingFlags.Static | BindingFlags.NonPublic, [typeof(Type)])!
            .Invoke(null, [type])!;

        for (var i = 0; i < initInfo.syncMemberFields.Length; i++)
        {
            var field = initInfo.syncMemberFields[i];
            var fieldName = initInfo.syncMemberNames[i];

            List<string>? oldNames = null;
            initInfo.oldSyncMemberNames?.TryGetValue(fieldName, out oldNames);

            yield return new TypeFieldsEntry(fieldName, field.FieldType, OldNames: oldNames);
        }
    }

    public static (string typeColumn, bool advanced) FormatFieldType(Type type, Type? containingType = null)
    {
        if (UnwrapSilentType(type) is { } contained)
        {
            // We don't need to use Template:RootFieldType here.
            // If we can let Template:Table ComponentFields handle it, we pass the name DIRECTLY.
            // Else we use advanced mode but still format the type manually.
            if (IsNonDefaultNamespace(contained) || contained.IsGenericType || contained.IsGenericTypeParameter || IsNestedType(contained, containingType))
            {
                // We bold ourselves, since nobody else will.
                return ($"'''{MakeDisplayType(contained, containingType)}'''", true);
            }

            return (SimpleTypeName(contained), false);
        }

        var rootType = type.IsGenericType ? type.GetGenericTypeDefinition() : type;

        if (IsNestedType(type, containingType))
        {
            return ($"{{{{RootFieldType|(nested)|{MakeDisplayType(type, containingType)}}}}}", true);
        }
        else
        {
            var genericParams = "";
            if (type.IsGenericType)
            {
                genericParams = "|" + string.Join(", ", type.GenericTypeArguments.Select(x => MakeDisplayType(x, containingType)));
            }

            return ($"{{{{RootFieldType|{SimpleTypeName(rootType)}{genericParams}}}}}", true);
        }
    }

    private static bool IsNestedType(Type type, Type? containingType)
    {
        return type.IsNested && type.DeclaringType == containingType;
    }

    private static Type? UnwrapSilentType(Type type)
    {
        if (type is not { IsGenericType: true, GenericTypeArguments.Length: 1 })
            return null;

        var baseType = type.GetGenericTypeDefinition();
        var typeArg = type.GenericTypeArguments[0];

        if (SilentFieldTypes.Contains(baseType))
            return typeArg;

        return null;
    }

    private static string GetTypeNamespace(Type type)
    {
        if (type.IsAbstract)
            return DefaultNamespace;

        foreach (var (baseType, ns) in TypeNamespaces)
        {
            if (type.IsAssignableTo(baseType))
                return ns;
        }

        return DefaultNamespace;
    }

    private static bool IsNonDefaultNamespace(Type type)
    {
        return GetTypeNamespace(type) != DefaultNamespace;
    }

    private static string MakeDisplayType(Type type, Type? containingType)
    {
        if (type.IsGenericParameter)
            return type.Name;

        var sb = new StringBuilder();
        // If this is a nested type of the type we're creating the page for,
        // we generate an internal anchor link instead.
        var typePage = IsNestedType(type, containingType)
            ? $"#{SimpleTypeName(type)}"
            : $"{GetTypeNamespace(type)}{SimpleTypeName(type)}";
        sb.Append($"[[{typePage}|{SimpleTypeName(type)}]]");

        if (type.IsConstructedGenericType)
        {
            sb.Append("&lt;");

            var first = true;
            foreach (var typeArg in type.GenericTypeArguments)
            {
                if (!first)
                    sb.Append(", ");

                first = false;
                sb.Append(MakeDisplayType(typeArg, containingType));
            }

            sb.Append("&gt;");
        }

        return sb.ToString();
    }

    private static string SimpleTypeName(Type type)
    {
        return SpecialTypeNames.TryGetValue(type, out var special) ? special : type.Name.Capitalize();
    }

    public sealed record TypeFieldsEntry(
        string Name,
        Type Type,
        string? Description = null,
        List<string>? OldNames = null);
}
