using System.Xml.Serialization;
using org.SpocWeb.root.Data.xmls.MathML.Attributes;

namespace org.SpocWeb.root.Data.xmls.MathML.Elements;

/// <summary> Places <see cref="Super"/> directly below and right to <see cref="Base"/> </summary>
/// <remarks>
/// 2 children: base, and under- or overscript.
/// <see cref="MOver"/> places the content DIRECTLY below each other.
/// </remarks>
[XmlRoot("msup")]
public class MSuperScript : MElement {

	[XmlAttribute("superscriptshift")]
	public MMeasuredLength Shift { get; set; }

	public override sbyte Arity => 2;
		
	public AMathMlNode Base => Children[0];

	public AMathMlNode Super => Children[1];

	/// <summary> No Braces needed, because '_' and '^' are parsed with Top Prio! </summary>
	public override TextWriter AppendTo(TextWriter sb, bool toAscii) {
		Base.AppendTo(sb, toAscii).Write('^');
		return Super.AppendTo(sb, toAscii); //
	}

}