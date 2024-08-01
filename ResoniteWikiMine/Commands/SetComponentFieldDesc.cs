using ResoniteWikiMine.Generation;
using ResoniteWikiMine.MediaWiki;
using static ResoniteWikiMine.Utility.ComponentBatchUpdater;

namespace ResoniteWikiMine.Commands;

public sealed class SetComponentFieldDesc : ICommand
{
    // Usage: SetComponentFieldDesc <base type> <field> <value>

    public async Task<int> Run(WorkContext context, string[] args)
    {
        FrooxLoader.InitializeFrooxWorker();

        var baseTypeName = args[0];
        var baseType = FrooxLoader.FindFrooxType(baseTypeName);
        if (baseType == null)
        {
            Console.WriteLine($"Unable to find base type: {baseTypeName}");
            return 1;
        }

        var fieldName = args[1];
        var fieldDesc = args[2];

        return UpdateComponentPages(
            context,
            page => IsEligibleType(page.Type, baseType),
            page => UpdatePageContent(page, fieldName, fieldDesc));
    }

    private static BatchUpdatePageResult? UpdatePageContent(BatchUpdatePage page, string fieldName, string fieldDesc)
    {
        var fieldsTemplate = PageContentParser.GetTemplateInPage(page.Content, "Table ComponentFields");
        if (fieldsTemplate == null)
        {
            Console.WriteLine($"Unable to find Table ComponentFields in page for {page.Name}");
            return null;
        }

        var fieldDescriptions = UpdateComponentPages.ParseComponentFields(fieldsTemplate);

        if (fieldDescriptions.TryGetValue(fieldName, out var existingDesc) && !IsEmptyFieldDesc(existingDesc))
        {
            Console.WriteLine($"Skipping field on {page.Name}: already has description");
            return null;
        }

        fieldDescriptions[fieldName] = fieldDesc;

        var newContent = UpdateComponentPages.SpliceString(
            page.Content,
            fieldsTemplate.Range,
            FieldFormatter.MakeComponentFieldsTemplate(page.Type, fieldDescriptions));

        return new BatchUpdatePageResult
        {
            NewContent = newContent,
            ChangeDescription = $"update '{fieldName}' description"
        };
    }

    private static bool IsEligibleType(Type type, Type baseType)
    {
        if (type.IsAssignableTo(baseType))
            return true;

        // Check generic children too.
        // e.g. materials are DynamicAssetProviderBase<Material>,
        // we want to be able to match DynamicAssetProviderBase<A>.

        if (type.GetInterfaces().Any(i => GenericTypeMatch(i, baseType)))
            return true;

        var parentTypeCheck = type;
        do
        {
            if (GenericTypeMatch(parentTypeCheck, baseType))
                return true;

            parentTypeCheck = parentTypeCheck.BaseType;
        } while (parentTypeCheck != null);

        return false;
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
