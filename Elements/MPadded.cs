using System.Xml.Serialization;
using org.SpocWeb.root.Data.xmls.MathML.Attributes;

namespace org.SpocWeb.root.Data.xmls.MathML.Elements;

public class MPadded : MElement {

	public MMeasuredLength Width { get; set; }

	public MMeasuredLength Height { get; set; }

	public MMeasuredLength Depth { get; set; }

	[XmlAttribute("lspace")]
	public MMeasuredLength LeftSpace { get; set; }

	[XmlAttribute("voffset")]
	public MMeasuredLength VerticalOffset { get; set; }

	[XmlIgnore] public override sbyte Arity => ImpliedMRowArity;

}