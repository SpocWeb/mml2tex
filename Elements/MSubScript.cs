using System.Xml.Serialization;
using org.SpocWeb.root.Data.xmls.MathML.Attributes;

namespace org.SpocWeb.root.Data.xmls.MathML.Elements {

	/// <summary> Places <see cref="Sub"/> directly below and right to <see cref="Base"/> </summary>
	/// <remarks>
	/// 2 children: base, and under- or over-script.
	/// <see cref="MUnder"/> places the content DIRECTLY below each other.
	/// </remarks>
	[XmlRoot("msub")]
	public class MSubScript : MElement {

		[XmlAttribute("subscriptshift")]
		public MMeasuredLength Shift { get; set; }

		public override sbyte Arity => 2;

		public AMathMlNode Base => Children[0];

		public AMathMlNode Sub => Children[1];

		/// <summary> No Braces needed, because '_' and '^' are parsed with Top Prio! </summary>
		public override TextWriter AppendTo(TextWriter sb, bool toAscii) {
			Base.AppendTo(sb, toAscii).Write('_');
			return Sub.AppendTo(sb, toAscii); //
		}

	}
}