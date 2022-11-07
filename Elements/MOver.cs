using System.ComponentModel;

namespace org.SpocWeb.root.Data.xmls.MathML.Elements;

/// <summary> Places <see cref="Over"/> directly above <see cref="Base"/> </summary>
/// <remarks>
/// 2 children: base, and under- or overscript.
/// <see cref="MSuperScript"/> places the content to the top right Corner.
/// </remarks>
public class MOver : MElement {

	public MOver() : this(false) { }

	public MOver(bool accent) => Accent = accent;

	[DefaultValue(false)]
	public bool Accent { get; set; }

	public override sbyte Arity => 2;

	public AMathMlNode Base => Children[0];

	public AMathMlNode Over => Children[1];

	public override TextWriter AppendTo(TextWriter sb, bool toAscii) {
		var symbol = Over.GetSymbol();
		var baseS = Base.GetSymbol();
		if (baseS?.Type == AsciiMath.Parser.Token.UnderOver) {
			Base.AppendTo(sb, toAscii).Write('^');
			return Over.AppendTo(sb, toAscii); //
		}
		if (symbol is not null 
		    &&(symbol.Type == AsciiMath.Parser.Token.Unary
		       //|| symbol.Type == AsciiMath.Parser.Token.Binary
		       || symbol.Type == AsciiMath.Parser.Token.UnaryUnderOver)) {
			Over.AppendTo(sb, toAscii); if (toAscii) {
				sb.Write('{'); //Function Application looks better with {arg}
			}
			Base.AppendTo(sb, toAscii); if (toAscii) {
				sb.Write('}'); //for bb{...} etc
			}
			return sb;
		}
		sb.Write(AsciiMath.Parser.TEX_OVER_SET); sb.Write('{');
		Over.AppendTo(sb, toAscii).Write("]["); //different Baces...
		Base.AppendTo(sb, toAscii).Write('}'); //...indicate binary PostFix Notation
		return sb;
	}

}