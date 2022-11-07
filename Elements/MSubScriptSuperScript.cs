using System.Xml.Serialization;
using org.SpocWeb.root.Data.xmls.MathML.Attributes;

namespace org.SpocWeb.root.Data.xmls.MathML.Elements;

/// <summary> Places <see cref="MSubScript.Sub"/> and <see cref="Super"/> right to <see cref="MSubScript.Base"/> </summary>
/// <remarks>
/// 3 children: base, and under- or overscript.
/// </remarks>
[XmlRoot("msubsup")]
public class MSubScriptSuperScript : MSubScript {

	public MMeasuredLength SubScriptShift { get; set; }

	public MMeasuredLength SuperScriptShift { get; set; }

	public override sbyte Arity => 3;

	public AMathMlNode Super => Children[2];

	/// <summary> No Braces needed, because '_' and '^' are parsed with Top Prio! </summary>
	public override TextWriter AppendTo(TextWriter sb, bool toAscii) {
		Base.AppendTo(sb, toAscii).Write('_'); //Base must NOT be empty! Rather write empty Braces {::}
		Sub.AppendTo(sb, toAscii).Write('^'); 
#warning TODO: don't know whether Sub or Super are empty => Errors!
		Super.AppendTo(sb, toAscii).Write(OpGlueNAry);
		return sb;
	}

}