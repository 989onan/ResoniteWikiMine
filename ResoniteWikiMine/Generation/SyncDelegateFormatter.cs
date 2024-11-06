using System.Reflection;
using System.Text;
using Elements.Core;
using FrooxEngine;
using Microsoft.Extensions.Primitives;

namespace ResoniteWikiMine.Generation;


public class SyncDelegateFormatter
{

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

    internal static string MakeSyncDelegatesTemplate(Type type, Dictionary<string, string>? descriptions = null)
    {
        var sb = new StringBuilder();
        sb.Append("{{Table ComponentTriggers\n");

        MakeSyncDelegatesCore(sb, type, SkipComponentFields, descriptions);

        sb.Append("}}");
        return sb.ToString();
    }


    /* trying to generate this:
    {{Table ComponentTriggers
    |Spawn()|[[Type:Action`1|Action]]| Spawns a slot randomly selected from <code>Templates</code>, located at the origin of this component's parent slot.
    |SpawnAtPoint()|[[Type:Action`1|Action`1]]&lt;[[Types:Float3|Float3]]&gt;| Spawns a slot randomly selected from <code>Templates</code>, located at the position specified in the <code>Float3</code> argument 
    }}*/

    private static void MakeSyncDelegatesCore(
        StringBuilder sb,
        Type type,
        string[] skipFields,
        Dictionary<string, string>? descriptions = null,
        Type? containingType = null)
    {
        var array = EnumerateFilteredSyncFields(type, skipFields);
        try//since check null was not working!!!!
        {
            array.Count();
        }
        catch
        {
            return;
        }
        var list = array.ToArray();
        for (var i = 0; i < list.Length; i++)
        {
            var field = list[i];
            var desc = GetDescription(field, descriptions);
            var (fieldType, advanced) = FormatFieldType(field);

            sb.Append($"|{field.Name + "()"}");
            sb.Append($"|{fieldType}");
            sb.Append($"|{desc}");

            sb.Append(field.Description);
            sb.Append('\n');
        }
    }

    private static string GetDescription(SyncDelegateEntry field, Dictionary<string, string>? descriptions)
    {
        if (descriptions == null)
            return "";

        if (descriptions.TryGetValue(field.Name + "()", out var desc) && !string.IsNullOrWhiteSpace(desc))
            return desc;

        if (field.OldNames != null)
        {
            foreach (var oldName in field.OldNames)
            {
                if (descriptions.TryGetValue(oldName + "()", out desc) && !string.IsNullOrWhiteSpace(desc))
                    return desc;
            }
        }

        return "";
    }

    private static IEnumerable<SyncDelegateEntry> EnumerateFilteredSyncFields(Type type, string[] skipFields)
    {
        return EnumerateSyncMethods(type).Where(tuple => Array.IndexOf(skipFields, tuple.Name) == -1);
    }

    public static IEnumerable<SyncDelegateEntry> EnumerateSyncMethods(Type type)
    {
        var initInfo = (WorkerInitInfo) typeof(WorkerInitializer)
            .GetMethod("GetInitInfo", BindingFlags.Static | BindingFlags.NonPublic, [typeof(Type)])!
            .Invoke(null, [type])!;

        for (var i = 0; i < initInfo.syncMethods.Length; i++)
        {
            var field = initInfo.syncMethods[i];
            var fieldName = initInfo.syncMethods[i].method.Name;

            List<string>? oldNames = null;
            initInfo.oldSyncMemberNames?.TryGetValue(fieldName, out oldNames);

            yield return new SyncDelegateEntry(fieldName, field.methodType, OldNames: oldNames, Arguments: new Dictionary<Type, string>(field.method.GetParameters().Select(p => new KeyValuePair<Type, string>(p.ParameterType, p.Name != null ? p.Name : "_"))));
        }
    }


    //format the sync delegate arguments into a list contained within '<' and '>' symbols
    public static (string typeColumn, bool advanced) FormatFieldType(SyncDelegateEntry type)
    {
        var sb = new StringBuilder();
        var str = FieldFormatter.MakeDisplayTypeNoNesting(type.ReturnType, null);
        sb.Append(str);
        if (type.Arguments.Count > 0)
        {
            sb.Append("&lt;");
            foreach (var keyvalue in type.Arguments)
            {
                var str2 = FieldFormatter.MakeDisplayType(keyvalue.Key, null);
                sb.Append(str2);
                sb.Append(": ");
                sb.Append(keyvalue.Value);
                sb.Append(", ");
            }
            sb.Remove(sb.Length - 2, 2);
            sb.Append("&gt;");
        }



        return (sb.ToString(), true);
    }

    private static string SimpleTypeName(Type type)
    {
        return SpecialTypeNames.TryGetValue(type, out var special) ? special : type.Name.Capitalize();
    }

    public sealed record SyncDelegateEntry(
        string Name,
        Type ReturnType,
        Dictionary<Type, string> Arguments,
        string? Description = null,
        List<string>? OldNames = null);
}

