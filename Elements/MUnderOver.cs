using System.ComponentModel;
using System.Xml.Serialization;
using org.SpocWeb.root.Data.xmls.MathML.Attributes;

namespace org.SpocWeb.root.Data.xmls.MathML.Elements;

/// <summary> Places <see cref="MUnder.Under"/> and <see cref="Over"/> directly below or above <see cref="MUnder.Base"/> </summary>
/// <remarks>
/// 3 children: base, and under- or overscript.
/// Used for large Operators like ∫∑⋃ etc.
/// By default, limits are displayed above and below when an expression is displayed by itself, 
/// and in the sub/super script positions when the expression is in a line of text. 
/// </remarks>
public class MUnderOver : MUnder {

	[XmlAttribute("accent")]
	[DefaultValue(false)]
	public bool AccentOver { get; set; }

	[DefaultValue(false)]
	public bool AccentUnder { get; set; }

	[DefaultValue(MHorizontalAlign.Center)]
	public MHorizontalAlign Align { get; set; }

	public override sbyte Arity => 3;

	public AMathMlNode Over => Children[2];

	public override TextWriter AppendTo(TextWriter sb, bool toAscii) {
		Base.AppendTo(sb, toAscii).Write('_');
		Under.AppendTo(sb, toAscii).Write('^');
#warning TODO: don't know whether Sub or Super are empty => Errors!
		Over.AppendTo(sb, toAscii).Write(OpGlueNAry);
		return sb;
	}

}