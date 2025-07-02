using Elements.Core;
using FrooxEngine;
using ResoniteWikiMine.Generation;
using ResoniteWikiMine.MediaWiki;
using static ResoniteWikiMine.Utility.ComponentBatchUpdater;

namespace ResoniteWikiMine.Commands;

public sealed class SetMaterialDescriptions : ICommand
{
    // Usage: SetComponentFieldDesc <base type> <field> <value>

    public async Task<int> Run(WorkContext context, string[] args)
    {

        var baseType = typeof(FrooxEngine.MaterialProvider);
        if (baseType == null)
        {
            //Console.WriteLine($"Unable to find base type: {baseType.Name}");
            return 1;
        }

        return UpdateComponentPages(
        context,
        page => IsEligibleType(page.Type, baseType),
        page => UpdatePageContent(page));

    }

   

    public static List<string> IgnoreThese = new List<string>()
    {
        "{{Asset HighPriorityIntegration Field}}",
        "{{Template:Material_StencilComparison_Desc}}",
        "{{Template:Material_StencilOperation_Desc}}",
        "{{Template:Material_StencilID_Desc}}",
        "{{Template:Material_StencilWriteMask_Desc}}",
        "{{Template:Material_StencilReadMask_Desc}}",
        "{{Template:Material_RenderQueue_Desc}}",
        "{{Template:Material_BlendMode_Desc}}",
        "{{Template:Material_Sidedness_Desc}}",
        "{{Template:Material_ZWrite_Desc}}",
        "{{Template:Material_ZTest_Desc}}",
        "{{Template:Material_BlendMode_Desc}}",
        "{{Template:Material_OffsetFactor_Desc}}",
        "{{Template:Material_OffsetUnits_Desc}}",
        "{{Template:Material_MultiValue_Desc}}",
        "{{Template:Material_AlphaCutoff_Desc}}",
        "{{Template:Material_Culling_Desc}}",
        "{{Material_OffsetFactor_Desc}}",
        "{{Material_OffsetUnits_Desc}}",
        "{{Material_RenderQueue_Desc}}",
        "{{Material_Metallic_Desc}}",
        "{{Material_Smoothness_Desc}}",
        "{{Template:Material_Metallic_Desc}}",
        "{{Template:Material_Smoothness_Desc}}",
        "{{Template:Material_Culling_Desc}}",
        "{{Template:HeightScale}}",
        "{{Template:MaterialSlice_HideSlicers_Desc}}",
        "{{Template:Material_Rect}}"
    };

    private static BatchUpdatePageResult? UpdatePageContent(BatchUpdatePage page)
    {
        //Console.WriteLine($"Doing {page.Name}");
        var fieldsTemplate = PageContentParser.GetTemplateInPage(page.Content, "Table ComponentFields");
        if (fieldsTemplate == null)
        {
            //Console.WriteLine($"Unable to find Table ComponentFields in page for {page.Name}");
            return null;
        }

        string changes = $"update ";

        var fieldDescriptions = UpdateComponentPage.ParseTableFields(fieldsTemplate);

        foreach(var field in fieldDescriptions.Keys)
        {

            if (IgnoreThese.Contains(fieldDescriptions[field].Trim()) || fieldDescriptions[field].Trim().Equals("{{Template:Material_" + field + "}}"))
            {
                //Console.WriteLine($"{{{{Template:{field}}}}}");
                continue;
            }
            fieldDescriptions[field] = "{{Template:Material_" + field + "}}";
            changes += $"'{field}' description,";
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
