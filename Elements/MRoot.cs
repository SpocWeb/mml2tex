namespace org.SpocWeb.root.Data.xmls.MathML.Elements {

	/// <summary> Used for both SqRt and Root; optional second child, which is displayed above the radical in the location of the n in an nth root. </summary>
	public class MRoot : MElement {

		public AMathMlNode Radicand => Children[0];

		public AMathMlNode Index => Children.Count > 1 ? Children[1] : new MGlyph();

		public override sbyte Arity => 2;

		public override TextWriter AppendTo(TextWriter sb, bool toAscii) {
			sb.Write("root("); //√ is ambiguous and not decidable in the open AsciiMath Syntax; the Op determines the Arity!
			Radicand.AppendTo(sb, toAscii).Write(')'); 
			if (Children.Count <= 1) {
				return sb;
			}
			sb.Write('(');
			Index.AppendTo(sb, toAscii).Write(')');
			return sb;
		}

	}
}