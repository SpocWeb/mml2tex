using System.ComponentModel;
using System.Xml.Serialization;
using org.SpocWeb.root.Data.xmls.MathML.Attributes;

namespace org.SpocWeb.root.Data.xmls.MathML.Elements;

/// <summary> MathMl Root Element </summary>
[XmlRoot("math")]
public class Math : MMultiElement { //MElement

	#region Enums not needed, since this Assembly is large enough to avoid Code Sharing
	#endregion Enums 

	public Math() { }

	public Math(MDisplay display) : this(display, MOverflow.LineBreak) { }

	public Math(MDisplay display, MOverflow overflow) {
		Display = display;
		Overflow = overflow;
	}

	[DefaultValue(MDisplay.Block)]
	public MDisplay Display { get; set; }

	[DefaultValue(MOverflow.LineBreak)]
	public MOverflow Overflow { get; set; }

	//[XmlIgnore] public override sbyte Arity => 1; //???

}