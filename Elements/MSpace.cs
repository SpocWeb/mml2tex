using System.ComponentModel;
using System.Xml.Serialization;
using org.SpocWeb.root.Data.xmls.MathML.Attributes;

namespace org.SpocWeb.root.Data.xmls.MathML.Elements {

	/// <summary> Has only Width and Height, no Children or Content </summary>
	public class MSpace : MElement { //AMathMlNode { //

		public MMeasuredLength Width { get; set; }

		public MMeasuredLength Height { get; set; }

		public MMeasuredLength Depth { get; set; }

		[DefaultValue(MLineBreak.Auto)]
		public MLineBreak LineBreak { get; set; }

		[XmlIgnore] public override sbyte Arity => 0;

		public override TextWriter AppendTo(TextWriter sb, bool toAscii) {
			sb.Write('\u00A0');
			return sb;
		}
	}
}