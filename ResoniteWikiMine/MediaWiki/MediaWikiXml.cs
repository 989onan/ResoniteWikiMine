using System.Diagnostics;
using System.Xml.Serialization;

namespace ResoniteWikiMine.MediaWiki;

[XmlRoot("mediawiki", Namespace = "http://www.mediawiki.org/xml/export-0.11/")]
public sealed class MediaWikiXmlRoot
{
    [XmlElement("page")] public required Page[] Pages;
}

[DebuggerDisplay("{Title}")]
public sealed class Page
{
    [XmlElement("title")] public required string Title;
}
