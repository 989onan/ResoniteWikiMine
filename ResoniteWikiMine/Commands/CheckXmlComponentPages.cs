using System.IO.Compression;
using System.Xml.Serialization;
using ResoniteWikiMine.MediaWiki;

namespace ResoniteWikiMine.Commands;

public sealed class CheckXmlComponentPages : ICommand
{
    public async Task<int> Run(WorkContext context, string[] args)
    {
        var xml = LoadXml();

        var count = xml.Pages.Count(x => x.Title.StartsWith("Component:"));
        Console.WriteLine($"Components: {count}");

        return 0;
    }

    private static MediaWikiXmlRoot LoadXml()
    {
        var path = @"D:\Downloads\wiki_db_xml_a47782fa2850309a1216.xml.gz";
        using var fs = File.OpenRead(path);
        using var gz = new GZipStream(fs, CompressionMode.Decompress, true);

        var serializer = new XmlSerializer(typeof(MediaWikiXmlRoot));
        return (MediaWikiXmlRoot) serializer.Deserialize(gz)!;
    }
}