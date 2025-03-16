using System.Reflection;
using System.Runtime;
using System.Text;
using Elements.Core;
using FrooxEngine;
using Microsoft.Extensions.Primitives;

namespace ResoniteWikiMine.Generation;


public class SyncDelegateFormatter
{


    // These "wrapping" field types are "invisible" with Template:RootFieldType.
    // This means we can directly have Table ComponentFields handle them,
    // instead of having to use an advanced type with Template:RootFieldType.
    // We could just elide this list and use Template:RootFieldType,
    // but that would bloat a ton of pages with non-complex field types,
    // and also it'd create a pretty major diff I don't wanna look over vs the initial wiki launch.

    internal static string MakeSyncDelegatesTemplate(Type type, Dictionary<string, string>? descriptions = null)
    {
        var sb = new StringBuilder();
        sb.Append("{{Table ComponentTriggers\n");

        MakeSyncDelegatesCore(sb, type, descriptions);

        sb.Append("}}");

        if (sb.ToString().Equals("{{Table ComponentTriggers\n}}"))
        {
            return "";
        }
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
        Dictionary<string, string>? descriptions = null,
        Type? containingType = null)
    {
        var array = EnumerateFilteredSyncFields(type);
        if (array == null)
        {
            return;
        }
        
        var list = array.ToArray();
        for (var i = 0; i < list.Length; i++)
        {
            if (type == typeof(FrooxEngine.Skybox))
            {
                Console.WriteLine("Skybox2!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            }
            //Console.WriteLine("creating sync delegate entry");
            var field = list[i];
            var desc = GetDescription(field, descriptions);
            var (fieldType, advanced) = FormatFieldType(field);

            sb.Append($"|{FormatName(field)}");
            sb.Append($"|{fieldType}");
            sb.Append($"|{desc}");
            sb.Append('\n');
        }
    }

    private static string GetDescription(SyncDelegateEntry field, Dictionary<string, string>? descriptions)
    {
        if (descriptions == null)
            return "";

        if (descriptions.TryGetValue(field.Name + "()", out var desc2) && !string.IsNullOrWhiteSpace(desc2))
        {
            //Console.WriteLine(field.Name + "()" + " on " + field.method.method.DeclaringType?.Name + " is the name of this sync delegate.");
            return desc2;
        }
        if (descriptions.TryGetValue(field.Name, out var desc5) && !string.IsNullOrWhiteSpace(desc5))
            return desc5;
        if (descriptions.TryGetValue(FormatName(field), out var desc) && !string.IsNullOrWhiteSpace(desc))
            return desc;

        if (field.OldNames != null)
        {
            foreach (var oldName in field.OldNames)
            {
                if (descriptions.TryGetValue(field.Name + "()", out var desc3) && !string.IsNullOrWhiteSpace(desc3))
                    return desc3;
                if (descriptions.TryGetValue(field.Name, out var desc6) && !string.IsNullOrWhiteSpace(desc6))
                    return desc6;
                if (descriptions.TryGetValue(FormatName(field), out var desc4) && !string.IsNullOrWhiteSpace(desc4))
                    return desc4;
            }
        }

        return "";
    }

    private static IEnumerable<SyncDelegateEntry> EnumerateFilteredSyncFields(Type type)
    {
        return EnumerateSyncMethods(type);
    }

    public static IEnumerable<SyncDelegateEntry> EnumerateSyncMethods(Type type)
    {
        SyncMethodInfo[] syncMethods = Enumerable.ToArray(Enumerable.Select(Enumerable.Where(type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public), (MethodInfo m) => m.GetCustomAttribute<SyncMethod>() != null), (MethodInfo m) => new SyncMethodInfo(m)));
        Dictionary<string, List<string>> oldSyncMemberNames = new Dictionary<string, List<string>>();
        foreach (var fieldInfo in syncMethods)
        {
            foreach (OldName item2 in Enumerable.Cast<OldName>(fieldInfo.method.GetCustomAttributes(typeof(OldName), inherit: false)))
            {

                if (!oldSyncMemberNames.TryGetValue(fieldInfo.method.Name, out var value))
                {
                    value = new List<string>();
                    oldSyncMemberNames.Add(fieldInfo.method.Name, value);
                }

                string[] oldNames = item2.OldNames;
                foreach (string item in oldNames)
                {
                    value.Add(item);
                }
            }
        }

        if (syncMethods == null)
        {
            if (type == typeof(FrooxEngine.Skybox))
            {
                Console.WriteLine("Skybox FAIL!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            }
            yield break;
        }

        for (var i = 0; i < syncMethods.Length; i++)
        {
            var field = syncMethods[i];
            var fieldName = syncMethods[i].method.Name;

            List<string>? oldNames = null;
            oldSyncMemberNames?.TryGetValue(fieldName, out oldNames);

            yield return new SyncDelegateEntry(fieldName, field, OldNames: oldNames, Arguments: new List<Tuple<Type,string>>(field.method.GetParameters().Select(p => new Tuple<Type, string>(p.ParameterType,p.Name != null ? p.Name : "_"))));
        }
    }


    //format the sync delegate arguments into a list contained within '<' and '>' symbols
    public static (string typeColumn, bool advanced) FormatFieldType(SyncDelegateEntry type)
    {
        var sb = new StringBuilder();
        var str = FieldFormatter.MakeDisplayTypeNoNesting(type.method.methodType, null);
        sb.Append(str);
        if (type.Arguments.Count > 0)
        {
            sb.Append("&lt;");
            foreach (var keyvalue in type.Arguments)
            {
                var str2 = FieldFormatter.MakeDisplayType(keyvalue.Item1, null);
                sb.Append(str2);
                sb.Append(": ");
                sb.Append(keyvalue.Item2);
                sb.Append(", ");
            }
            sb.Remove(sb.Length - 2, 2);
            sb.Append("&gt;");
        }
        sb.Append($" -> {FieldFormatter.MakeDisplayType(type.method.method.ReturnType, null)}");
        if ((type.method.methodType == typeof(Delegate) || !type.method.method.IsPublic))
        {
            sb.Append(" : HIDDEN METHOD");
        }

        //if (type.Name == "SetActive" && type.method.method.DeclaringType == typeof(FrooxEngine.Skybox))
        //{
        //    Console.WriteLine(sb.ToString());
        //}


        return (sb.ToString(), true);
    }

    public static string FormatName(SyncDelegateEntry type)
    {
        var sb = new StringBuilder();
        var str = type.Name;
        sb.Append(str);
        sb.Append("(");
        if (type.Arguments.Count > 0)
        {
            foreach (var keyvalue in type.Arguments)
            {
                var str2 = keyvalue.Item1.GetNiceName();
                sb.Append(str2);
                sb.Append(", ");
            }
            sb.Remove(sb.Length - 2, 2);
        }
        sb.Append(")");


        return sb.ToString();
    }

    public sealed record SyncDelegateEntry(
        string Name,
        SyncMethodInfo method,
        List<Tuple<Type, string>> Arguments,
        List<string>? OldNames = null
        );
}

