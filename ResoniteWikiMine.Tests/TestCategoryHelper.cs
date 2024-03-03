using MwParserFromScratch;
using ResoniteWikiMine.MediaWiki;

namespace ResoniteWikiMine.Tests;

[TestFixture]
[TestOf(typeof(CategoryHelper))]
[Parallelizable(ParallelScope.All)]
public sealed class TestCategoryHelper
{
    [Test]
    public void Test()
    {
        // Parse
        const string text = """
            == foobar ==
            lorem ipsum

            [[Category:Foobar]]
            [[Category:Foobarbaz|lipsum]]
            """;

        var parser = new WikitextParser();
        var parsed = parser.Parse(text);
        var categories = CategoryHelper.GetCategories(parsed);

        // Verify parsed contents

        Assert.That(categories.Categories, Has.Count.EqualTo(2));
        Assert.That(categories.Categories[0].Target.ToPlainText(), Is.EqualTo("Category:Foobar"));
        Assert.That(categories.Categories[0].Text, Is.Null);
        Assert.That(categories.Categories[1].Target.ToPlainText(), Is.EqualTo("Category:Foobarbaz"));
        Assert.That(categories.Categories[1].Text.ToPlainText(), Is.EqualTo("lipsum"));

        // Try remove

        CategoryHelper.RemoveCategory(categories, categories.Categories[1]);

        // Yeah so like the code currently leaves a trailing newline but I don't really care :)
        Assert.That(parsed.ToString(), Is.EqualTo("""
            == foobar ==
            lorem ipsum

            [[Category:Foobar]]

            """));
    }
}
