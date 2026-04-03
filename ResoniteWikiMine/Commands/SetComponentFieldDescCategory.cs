using Elements.Core;
using ResoniteWikiMine.Generation;
using ResoniteWikiMine.MediaWiki;
using static ResoniteWikiMine.Utility.ComponentBatchUpdater;

namespace ResoniteWikiMine.Commands;

public sealed class SetComponentFieldDescCategory : ICommand
{
    // Usage: SetComponentFieldDesc <base type> <field> <value>

    public async Task<int> Run(WorkContext context, string[] args)
    {

        var baseTypeName = args[0];
        var replacedescriptions = args[1];

        bool replace = false;

        if (replacedescriptions.ToLower().Equals("true"))
        {
            replace = true;
        }

        if (args.Length.IsEven())
        {
            FrooxLoader.InitializeFrooxWorker();
            return UpdateComponentPages(
            context,
            page => IsEligibleType(page.Type, baseTypeName),
            page => UpdatePageContent(page, args.ToList().GetRange(2, args.Length - 2), replace));
        }
        else
        {
            Console.WriteLine($"need pairs of field names and descriptions.");
            return 1;
        }

    }

    private static BatchUpdatePageResult? UpdatePageContent(BatchUpdatePage page, List<string> pairs, bool replace)
    {
        var fieldsTemplate = PageContentParser.GetTemplateInPage(page.Content, "Table ComponentFields");
        if (fieldsTemplate == null)
        {
            Console.WriteLine($"Unable to find Table ComponentFields in page for {page.Name}");
            return null;
        }

        string changes = $"update ";

        var fieldDescriptions = UpdateComponentPage.ParseTableFields(fieldsTemplate);

        foreach(var fields in pairs.SplitToGroups(2))
        {

            if (fieldDescriptions.TryGetValue(fields[0], out var existingDesc) && !IsEmptyFieldDesc(existingDesc) && !replace)
            {
                Console.WriteLine($"Skipping field '{fields[0]}' on '{page.Name}': already has description");
                continue;
            }
            fieldDescriptions[fields[0]] = fields[1];
            changes += $"'{fields[0]}' description,";
        }
        var newContent = UpdateComponentPage.SpliceString(
            page.Content,
            fieldsTemplate.Range,
            FieldFormatter.MakeComponentFieldsTemplate(page.Type, fieldDescriptions));


        return new BatchUpdatePageResult
        {
            NewContent = newContent,
            ChangeDescription = changes
        };
    }

    private static bool IsEligibleType(Type type, string baseTypeCategory)
    {
        FrooxEngine.CategoryAttribute categories = (type.GetCustomAttributes(typeof(FrooxEngine.CategoryAttribute), true).FirstOrDefault<object>() as FrooxEngine.CategoryAttribute);
        string[] array;
        if ((array = ((categories != null) ? categories.Paths : null)) == null)
        {
            return false;
        }
        else
        {
            return baseTypeCategory == array.FirstOrDefault<string>();
        }
    }

    private static bool GenericTypeMatch(Type constructedType, Type genericType)
    {
        return constructedType.IsGenericType && constructedType.GetGenericTypeDefinition() == genericType;
    }

    private static bool IsEmptyFieldDesc(string desc)
    {
        return string.IsNullOrWhiteSpace(desc)
               || string.Equals(desc.Trim(), "{{Stub}}", StringComparison.InvariantCultureIgnoreCase);
    }
}
