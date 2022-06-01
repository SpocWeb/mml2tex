using System.ComponentModel;
using System.Xml.Serialization;
using org.SpocWeb.root.Data.xmls.MathML.Attributes;

namespace org.SpocWeb.root.Data.xmls.MathML.Elements {

	/// <summary> Unary or binary Operator </summary>
	/// <remarks>
	/// Contents should be displayed as {b operators}, 
	/// but how operators are displayed is often quite complicated. 
	/// For example, the spacing around operators varies depending on the operator. 
	/// Other operators like sums and products have 
	/// special conventions for displaying limits as scripts. 
	/// Still other operators like vertical rules stretch 
	/// to match the size of the expression which they enclose.
	/// 
	/// Also used to mark-up other symbols which are only operators in a very general sense, 
	/// but whose layout properties are like those of an operator. 
	/// Thus, mo elements are used to mark-up delimiter characters like 
	/// parentheses (which stretch), 
	/// punctuation (which has uneven spacing around it) and 
	/// accents (which also stretch)}.
	/// 
	/// Everything about how an operator should be displayed 
	/// can be controlled directly by using Attributes like 
	/// <see cref="LeftSpace"/>, <see cref="RightSpace"/>, <see cref="Stretchy"/>, and <see cref="MovableLimits"/>.
	/// </remarks>
	[XmlRoot("mo")]
	public class MOperator : MMultiElement { //MElement

		[DefaultValue(false)]
		public bool Accent { get; set; }

		[DefaultValue(false)]
		public bool Fence { get; set; }

		[DefaultValue(MOperatorForm.Infix)]
		public MOperatorForm Form { get; set; }

		[XmlAttribute("largeop")]
		[DefaultValue(false)]
		public bool LargeOperator { get; set; }

		/// <summary> Flag that this Operator has Limits in <see cref="MUnderOver"/> </summary>
		[DefaultValue(false)]
		public bool MovableLimits { get; set; }

		[DefaultValue(false)]
		public bool Separator { get; set; }

		[DefaultValue(false)]
		public bool Stretchy { get; set; }

		[DefaultValue(false)]
		public bool Symmetric { get; set; }

		[XmlAttribute("lspace")]
		public MMeasuredLength LeftSpace { get; set; }

		[XmlAttribute("rspace")]
		public MMeasuredLength RightSpace { get; set; }

	}
}