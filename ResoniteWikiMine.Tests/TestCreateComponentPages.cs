using FrooxEngine;
using FrooxEngine.FinalIK;
using ResoniteWikiMine.Commands;

namespace ResoniteWikiMine.Tests;

[TestFixture]
[TestOf(typeof(CreateComponentPages))]
[Parallelizable(ParallelScope.All)]
public sealed class TestCreateComponentPages
{
    [Test]
    [TestCase(typeof(XiexeToonMaterial), ExpectedResult = "Xiexe Toon Material")]
    [TestCase(typeof(VRIKAvatar), ExpectedResult = "VRIKAvatar")]
    [TestCase(typeof(PBS_ColorMaskMetallic), ExpectedResult = "PBS Color Mask Metallic")]
    public string TestGetNiceName(Type type)
    {
        return CreateComponentPages.GetNiceName(type);
    }
}
