using ResoniteWikiMine.MediaWiki;

namespace ResoniteWikiMine.Tests;

[TestFixture]
[TestOf(typeof(PageContentParser))]
[Parallelizable(ParallelScope.All)]
public sealed class TestPageContentParser
{
    [Test]
    public void TestGetTemplateInPage()
    {
        var text = """
            Example using TypeString to specify a different type from the link. (cropped from [[Component:ScaleObjectCreator]])

            {{Table ComponentFields
            |Manager|ScaleObjectManager|
            |_sizeParser|QuantityTextEditorParser`1|TypeString1=QuantityTextEditorParser<Distance>|
            |_material|FresnelMaterial|
            }}
            """.ReplaceLineEndings("\n");

        var match = PageContentParser.GetTemplateInPage(text, "Table ComponentFields");

        Assert.That(match!, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(match!.PositionalArguments, Is.EquivalentTo(new[]{"Manager", "ScaleObjectManager", "\n", "_sizeParser", "QuantityTextEditorParser`1", "\n", "_material", "FresnelMaterial", "\n"}));
            Assert.That(match!.NamedArguments, Is.EquivalentTo(new[]{KeyValuePair.Create("TypeString1", "QuantityTextEditorParser<Distance>")}));
        });
    }

    [Test]
    public void TestGetTemplateInPage2()
    {
        var text = """
            <languages></languages>
            <translate>
            <!--T:1-->
            {{stub}}
            [[File:Hyperlink Component.webp|thumb|The Hyperlink as seen in the [[Scene Inspector]]]]
            The '''Hyperlink''' component allows you to turn an object with a collider into a clickable link that will prompt the user in [[Userspace]] to open the provided link in their web browser. <!--T:2-->
            == Fields ==
            {{Table ComponentFields
            |URL|Uri|The hyperlink to open.
            |Reason|String|The reason that the hyperlink is being opened. Displayed to the user when they click it in the security dialog.
            |OpenOnce|Bool|
            }}

            <!--T:3-->
            == Usage ==

            <!--T:4-->
            == Examples ==

            <!--T:5-->
            == Related Components ==
            </translate>
            [[Category:ComponentStubs]]
            [[Category:Components{{#translation:}}|Hyperlink]]
            [[Category:Components:Utility{{#translation:}}|Hyperlink]]
            """.ReplaceLineEndings("\n");

        var match = PageContentParser.GetTemplateInPage(text, "Table ComponentFields");

        Assert.That(match!, Is.Not.Null);
    }
}
