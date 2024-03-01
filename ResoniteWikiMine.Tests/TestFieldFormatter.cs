using FrooxEngine;
using FrooxEngine.FinalIK;
using ResoniteWikiMine.Generation;

namespace ResoniteWikiMine.Tests;

[TestFixture]
[TestOf(typeof(FieldFormatter))]
[Parallelizable(ParallelScope.All)]
public sealed class TestFieldFormatter
{
    [Test]
    [TestCase(typeof(Sync<float>), "Float", false)]
    [TestCase(typeof(Sync<string>), "String", false)]
    [TestCase(typeof(SyncRef<Slot>), "Slot", false)]
    [TestCase(typeof(SyncArray<float>), "{{RootFieldType|SyncArray`1|[[Type:Float|Float]]}}", true)]
    [TestCase(typeof(SyncRef<VRIK>), "[[Component:VRIK|VRIK]]", true)]
    [TestCase(typeof(Sync<int?>), "[[Type:Nullable`1|Nullable`1]]&lt;[[Type:Int|Int]]&gt;", true)]
    public void TestFormatFieldType(Type type, string expected, bool expectedAdvanced)
    {
        var (text, advanced) = FieldFormatter.FormatFieldType(type);

        Assert.Multiple(() =>
        {
            Assert.That(text, Is.EqualTo(expected));
            Assert.That(advanced, Is.EqualTo(expectedAdvanced));
        });
    }

    [Test]
    [TestCase(typeof(SyncRef<InteractionHandlerPermissions.ToolRule>), typeof(InteractionHandlerPermissions), "'''[[#ToolRule|ToolRule]]'''", true)]
    [TestCase(typeof(Sync<VolumeUnlitMaterial.DisplayMode>), typeof(VolumeUnlitMaterial), "'''[[#DisplayMode|DisplayMode]]'''", true)]
    public void TestFormatFieldTypeContaining(Type type, Type containing, string expected, bool expectedAdvanced)
    {
        var (text, advanced) = FieldFormatter.FormatFieldType(type, containing);

        Assert.Multiple(() =>
        {
            Assert.That(text, Is.EqualTo(expected));
            Assert.That(advanced, Is.EqualTo(expectedAdvanced));
        });
    }
}
