using System.ComponentModel;
using System.Xml.Serialization;
using org.SpocWeb.root.Data.xmls.MathML.Attributes;

namespace org.SpocWeb.root.Data.xmls.MathML.Elements {

	public class MEnclose : MElement {

		public MEnclose() { }

		public MEnclose(MEncloseNotation notation) => Notation = notation;

		[DefaultValue(MEncloseNotation.LongDivision)]
		public MEncloseNotation Notation { get; set; }

		[XmlIgnore] public override sbyte Arity => ImpliedMRowArity;

		public override TextWriter AppendTo(TextWriter sb, bool toAscii) {
			string function = NewMethod();
			sb.Write(function); sb.Write('(');
			Children[0].AppendTo(sb, toAscii).Write(')');
			return sb;
		}

		string NewMethod() {
			switch (Notation) {
				case MEncloseNotation.DownDiagonalStrike: 
				case MEncloseNotation.HorizontalStrike: 
				case MEncloseNotation.UpDiagonalStrike: return "cancel";
				case MEncloseNotation.VerticalStrike: break;
				case MEncloseNotation.LongDivision: return "";
				case MEncloseNotation.Actuarial: break;
				case MEncloseNotation.Radical: break;
				case MEncloseNotation.Box: break;
				case MEncloseNotation.RoundedBox: break;
				case MEncloseNotation.Circle: break;
				case MEncloseNotation.Left: break;
				case MEncloseNotation.Right: break;
				case MEncloseNotation.Top: break;
				case MEncloseNotation.Bottom: break;
				case MEncloseNotation.Madruwb: break;
				case MEncloseNotation.UpDiagonalArrow: break;
				case MEncloseNotation.PhasOrAngle: break;
				default: throw new ArgumentOutOfRangeException();
			}
			throw new NotSupportedException(Notation + "");
		}
	}
}