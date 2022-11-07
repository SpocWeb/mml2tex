using System.Xml.Serialization;

namespace org.SpocWeb.root.Data.xmls.MathML.Elements;

/// <summary> accepts any number of children, and displays them under a radical sign '√' </summary>
[XmlRoot("msqrt")]
public class MSquareRoot : MElement {


	[XmlIgnore] public override sbyte Arity => ImpliedMRowArity;

	public override TextWriter AppendTo(TextWriter sb, bool toAscii) {
		sb.Write("√(");
		Children[0].AppendTo(sb, toAscii).Write(')');
		return sb;
	}

}