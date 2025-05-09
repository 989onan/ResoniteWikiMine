using System.Linq.Expressions;
using System.Reflection;
using System.Runtime;
using System.Text;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.Undo;
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
            //Console.WriteLine("creating sync delegate entry");
            var field = list[i];
            var desc = GetDescription(field, descriptions);
            var (fieldType, advanced) = FormatFieldType(field);

            sb.Append($"|{FormatName(field)}");
            sb.Append($"|{fieldType}");
            if ((field.method.methodType == typeof(Delegate) || !field.method.method.IsPublic))
            {
                sb.Append("|true");
            }
            else
            {
                sb.Append("|false");
            }
            sb.Append($"|{desc}");
            sb.Append('\n');
        }
    }

    private static string GetDescription(SyncDelegateEntry field, Dictionary<string, string>? descriptions)
    {
        if (descriptions == null)
            return "";

        if (descriptions.TryGetValue(FormatName(field), out var desc0) && !string.IsNullOrWhiteSpace(desc0))
            return desc0;
        if (descriptions.TryGetValue(FormatNameOld1(field), out var desc3) && !string.IsNullOrWhiteSpace(desc3))
            return desc3;

        if (field.OldNames != null)
        {
            foreach (var oldName in field.OldNames)
            {
                if (descriptions.TryGetValue(FormatName(field).Replace(field.Name, oldName), out var desc1) && !string.IsNullOrWhiteSpace(desc1))
                    return desc1;
                if (descriptions.TryGetValue (FormatNameOld1(field).Replace(field.Name, oldName), out var desc2) && !string.IsNullOrWhiteSpace(desc2))
                    return desc2;
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

        //if (type.Name == "SetActive" && type.method.method.DeclaringType == typeof(FrooxEngine.Skybox))
        //{
        //    Console.WriteLine(sb.ToString());
        //}


        return (FieldFormatter.MakeDisplayType(Helper.ClassifyDelegate(type.method.method), null), true);
    }

    public static string FormatName(SyncDelegateEntry type)
    {
        return type.Name + ":" + FieldFormatter.MakeDisplayType(Helper.ClassifyDelegate(type.method.method), null);
    }


    public static string FormatNameOld1(SyncDelegateEntry type)
    {
        return type.Name + "" + FieldFormatter.MakeDisplayType(Helper.ClassifyDelegate(type.method.method), null);
    }

    public sealed record SyncDelegateEntry(
        string Name,
        SyncMethodInfo method,
        List<Tuple<Type, string>> Arguments,
        List<string>? OldNames = null
        );
    //shamefully stolen from Art0007i's code

    public struct MethodArgs
    {
        public Type returnType;
        public Type[] argumentTypes;

        public MethodArgs(Type returnType, Type[] argumentTypes)
        {
            this.returnType = returnType;
            this.argumentTypes = argumentTypes;
        }
        public MethodArgs(params Type[] argumentTypes)
        {
            this.returnType = typeof(void);
            this.argumentTypes = argumentTypes;
        }
        public MethodArgs(MethodInfo source)
        {
            this.returnType = source.ReturnType;
            this.argumentTypes = source.GetParameters().Select((f) => f.ParameterType).ToArray();
        }

        public override string ToString()
        {
            var rett = returnType == null ? "void" : returnType.Name;
            var args = string.Join(", ", argumentTypes.Select(t => t.Name));
            return $"{rett} ({args})";
        }

        public override bool Equals(object obj)
        {
            if (obj is MethodArgs y)
            {
                var x = this;
                if (x.returnType != y.returnType) return false;
                if (x.argumentTypes.Length != y.argumentTypes.Length) return false;
                for (int i = 0; i < x.argumentTypes.Length; i++)
                {
                    if (x.argumentTypes[i] != y.argumentTypes[i]) return false;
                }
                return true;
            }
            return base.Equals(obj);
        }
        public static bool operator ==(MethodArgs lhs, MethodArgs rhs)
        {
            return lhs.Equals(rhs);
        }
        public static bool operator !=(MethodArgs lhs, MethodArgs rhs)
        {
            return !lhs.Equals(rhs);
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17; // Start with a prime number

                // Combine the hash code of the returnType
                hash = hash * 23 + (returnType != null ? returnType.GetHashCode() : 0);

                // Combine the hash codes of each argument type in the array
                if (argumentTypes != null)
                {
                    foreach (var argType in argumentTypes)
                    {
                        hash = hash * 23 + (argType != null ? argType.GetHashCode() : 0);
                    }
                }

                return hash;
            }
        }
    }
    internal class Helper
    {
        public static Dictionary<MethodArgs, Type> argumentLookup = new()
    {
        { new(typeof(IButton), typeof(ButtonEventData)), typeof(ButtonEventHandler) }, // used literally everywhere lol
        { new(typeof(string), typeof(SyncObject)), typeof(SubsettingGetter) }, // used in settings items
        { new(typeof(bool), new Type[] {typeof(IGrabbable), typeof(Grabber) }), typeof(GrabCheck) }, // Used in Grabbable.UserRootGrabCheck
        //{ new(typeof(bool), new Type[] {} ), typeof(Func<bool>) }, // Used in SlotInspector.IsTargetEmpty
        //{ new(typeof(TextEditor)), typeof(Action<TextEditor>) }, // Used in FieldEditor.EditingFinished, FieldEditor.EditingChanged, FieldEditor.EditingStarted
        { new(typeof(ITouchable), typeof(TouchEventInfo).MakeByRefType()), typeof(TouchEvent) },
        { new(typeof(ITouchable), new Type[] {typeof(RelayTouchSource), typeof(float3).MakeByRefType(), typeof(float3).MakeByRefType(), typeof(float3).MakeByRefType(), typeof(bool).MakeByRefType()}), typeof(TouchableGetter) },
        { new(typeof(SlotGizmo), typeof(SlotGizmo)), typeof(SlotGizmo.SlotGizmoReplacement) },
        { new(typeof(LegacyWorldItem)), typeof(LegacyWorldItemAction) },
        //{ new(typeof(void), new Type[] {}), typeof(Action) }, // Used in WorldCloseDialog.Close 
        // Action<LocomotionController> // used in somewhere?

    };

        public static Type ClassifyDelegate(MethodInfo m)
        {
            if (argumentLookup.TryGetValue(new(m), out var t))
            {
                return t;
            }
            var p = m.GetParameters().Select(para => {
                if (para.ParameterType.IsByRefLike || para.ParameterType.IsByRef)
                {
                    return para.ParameterType.GetElementType();
                }
                else
                {
                    return para.ParameterType;
                }


            }).ToArray();
            if (p.Length == 3 && p[0] == typeof(IButton) && p[1] == typeof(ButtonEventData))
            {
                return typeof(ButtonEventHandler<>).MakeGenericType(p[2]);
            }
            return GetFuncOrAction(m, p);

        }

        public static Type GetFuncOrAction(MethodInfo m, Type[] p)
        {
            if (m.ReturnType == typeof(void))
            {
                Type[] j = new Type[p.Length];
                for (int i = 0; i < p.Length; i++)
                {
                    j[i] = p[i];
                }
                return Expression.GetActionType(p);
            }
            else
            {
                p = p.Concat(new[] { m.ReturnType }).ToArray();

                return Expression.GetFuncType(p);
            }
        }
    }
}

