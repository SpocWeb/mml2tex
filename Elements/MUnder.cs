using System.ComponentModel;
using System.Xml.Serialization;

namespace org.SpocWeb.root.Data.xmls.MathML.Elements;

/// <summary> Places <see cref="Under"/> directly below <see cref="Base"/> </summary>
/// <remarks>
/// 2 children: base, and under- or overscript.
/// <see cref="MSubScript"/> places the content to the bottom right Corner.
/// </remarks>
public class MUnder : MElement {

	public MUnder() : this(false) { }

	public MUnder(bool accent) => Accent = accent;

	[XmlAttribute("accentunder")]
	[DefaultValue(false)]
	public bool Accent { get; set; }

	public override sbyte Arity => 2; 

	public AMathMlNode Base => Children[0];

	public AMathMlNode Under => Children[1];

	public override TextWriter AppendTo(TextWriter sb, bool toAscii) {
		var under = Under.GetSymbol();
		var baseS = Base.GetSymbol();
		if (baseS?.Type == AsciiMath.Parser.Token.UnderOver) {
			Base.AppendTo(sb, toAscii).Write('_');
			return Under.AppendTo(sb, toAscii); //
		}
		if (under is not null 
		    &&(under.Type == AsciiMath.Parser.Token.Unary
		       //|| symbol.Type == AsciiMath.Parser.Token.Binary
		       || under.Type == AsciiMath.Parser.Token.UnaryUnderOver)) {
			Under.AppendTo(sb, toAscii); if (toAscii) {
				sb.Write('{'); //Function Application looks better with {arg}
			}
			Base.AppendTo(sb, toAscii); if (toAscii) {
				sb.Write('}'); //for bb{...} etc
			}
			return sb; //unary use! either with Space or with {...}
		}
		sb.Write(AsciiMath.Parser.TEX_UNDER_SET); sb.Write('{');
		Under.AppendTo(sb, toAscii).Write("]["); //different Baces...
		Base.AppendTo(sb, toAscii).Write('}'); //...indicate binary PostFix Notation
		return sb;
	}
}