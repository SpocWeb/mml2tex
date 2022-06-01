using System.ComponentModel;
using System.Xml.Serialization;
using org.SpocWeb.root.Data.xmls.MathML.Attributes;

namespace org.SpocWeb.root.Data.xmls.MathML.Elements {

	/// <summary> Container like <see cref="MRow"/>, but used to set any MathML attribute. </summary>
	public class MStyle : MMultiElement {

		[DefaultValue(true)]
		public bool DisplayStyle { get; set; }

		[DefaultValue('.')]
		public char DecimalPoint { get; set; }

		[DefaultValue(MInfixLineBreakStyle.Before)]
		public MInfixLineBreakStyle InfixLineBreakStyle { get; set; }

		[DefaultValue(0)]
		public int ScriptLevel { get; set; }

		public MMeasuredLength ScriptMinSize { get; set; }

		[DefaultValue(0.71)]
		public double ScriptSizeMultiplier { get; set; }

		[XmlIgnore] public override sbyte Arity => ImpliedMRowArity;

	}
}