using System.Xml.Serialization;
using org.SpocWeb.root.Data.xmls.MathML.Attributes;

namespace org.SpocWeb.root.Data.xmls.MathML.Elements;

public class MGlyph : MElement {

	[XmlAttribute("src")]
	public string Source { get; set; }

	[XmlAttribute("alt")]
	public string AlternativeText { get; set; }

	public MMeasuredLength Width { get; set; }

	public MMeasuredLength Height { get; set; }

	[XmlAttribute("valign")]
	public MMeasuredLength VerticalAlign { get; set; }

	/// <summary> Exactly 1 Text Node </summary>
	[XmlIgnore] public override sbyte Arity => 1; 

}