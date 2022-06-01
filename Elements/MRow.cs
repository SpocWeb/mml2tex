namespace org.SpocWeb.root.Data.xmls.MathML.Elements {

	/// <summary> mrow is for grouping together terms into a single unit. </summary>
	/// <remarks>
	/// Use <see cref="MFenced"/> to define Brackets. 
	/// The mrow element can contain any number of child elements, 
	/// which it displays aligned along the baseline in a horizontal row. 
	/// One might do this in order to make a collection of expressions into a single subscript, 
	/// or one might nest some terms in an mrow to limit how much a stretchy operator grows, and so on. 
	/// </remarks>
	public class MRow : MMultiElement {
		public MRow() {
			_Open = "("; //"{:";
			_Separators = " "; //",";
			_Close = ")"; //":}";
		}
	}
}