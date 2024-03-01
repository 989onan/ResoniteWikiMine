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
    private static readonly string[] SkipFields = ["Enabled", "UpdateOrder", "persistent"];

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
        (typeof(IMaterialBaseAsset), ""),
        (typeof(Component), "Component:"),
    ];

    internal static string MakeComponentFieldsTemplate(Type type, Dictionary<string, string>? descriptions = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("{{Table ComponentFields");

        MakeFieldsTemplateCore(sb, type, descriptions);

        sb.Append("}}");
        return sb.ToString();
    }

    internal static string MakeTypeFieldsTemplate(Type type, Dictionary<string, string>? descriptions = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("{{Table TypeFields");

        MakeFieldsTemplateCore(sb, type, descriptions);

        sb.Append("}}");
        return sb.ToString();
    }

    private static void MakeFieldsTemplateCore(StringBuilder sb, Type type, Dictionary<string, string>? descriptions = null)
    {
        var list = GetComponentFields(type);
        for (var i = 0; i < list.Count; i++)
        {
            var field = list[i];
            var desc = descriptions?.GetValueOrDefault(field.Name);
            var (fieldType, advanced) = FormatFieldType(field.Type);

            sb.Append($"|{field.Name}");
            sb.Append($"|{fieldType}");
            if (advanced)
                sb.Append($"|TypeAdv{i}=true");
            sb.Append($"|{desc}");

            sb.AppendLine(field.Description);
        }
    }

    private static List<ComponentFieldsEntry> GetComponentFields(Type type)
    {
        var initInfo = (WorkerInitInfo)typeof(WorkerInitializer)
            .GetMethod("GetInitInfo", BindingFlags.Static | BindingFlags.NonPublic, [typeof(Type)])!
            .Invoke(null, [type])!;

        var list = new List<ComponentFieldsEntry>();

        for (var i = 0; i < initInfo.syncMemberFields.Length; i++)
        {
            var field = initInfo.syncMemberFields[i];
            var fieldName = initInfo.syncMemberNames[i];
            if (Array.IndexOf(SkipFields, fieldName) != -1)
                continue;

            list.Add(new ComponentFieldsEntry(fieldName, field.FieldType));
        }

        return list;
    }

    public static (string typeColumn, bool advanced) FormatFieldType(Type type)
    {
        if (UnwrapSilentType(type) is { } contained)
        {
            // We don't need to use Template:RootFieldType here.
            // If we can let Template:Table ComponentFields handle it, we pass the name DIRECTLY.
            // Else we use advanced mode but still format the type manually.
            if (IsNonDefaultNamespace(contained) || contained.IsGenericType || contained.IsGenericTypeParameter)
            {
                // We bold ourselves, since nobody else will.
                return ($"'''{MakeDisplayType(contained)}'''", true);
            }

            return (SimpleTypeName(contained), false);
        }

        var rootType = type.IsGenericType ? type.GetGenericTypeDefinition() : type;

        var genericParams = "";
        if (type.IsGenericType)
        {
            genericParams = "|" + string.Join(", ", type.GenericTypeArguments.Select(MakeDisplayType));
        }

        return ($"{{{{RootFieldType|{SimpleTypeName(rootType)}{genericParams}}}}}", true);
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

    private static string MakeDisplayType(Type type)
    {
        if (type.IsGenericParameter)
            return type.Name;

        var sb = new StringBuilder();
        sb.Append($"[[{GetTypeNamespace(type)}{SimpleTypeName(type)}|{SimpleTypeName(type)}]]");

        if (type.IsConstructedGenericType)
        {
            sb.Append("&lt;");

            var first = true;
            foreach (var typeArg in type.GenericTypeArguments)
            {
                if (!first)
                    sb.Append(", ");

                first = false;
                sb.Append(MakeDisplayType(typeArg));
            }

            sb.Append("&gt;");
        }

        return sb.ToString();
    }

    private static string SimpleTypeName(Type type)
    {
        return SpecialTypeNames.TryGetValue(type, out var special) ? special : type.Name.Capitalize();
    }

    private sealed record ComponentFieldsEntry(
        string Name,
        Type Type,
        string? Description = null);
}
