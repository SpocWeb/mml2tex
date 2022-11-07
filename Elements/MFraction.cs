using System.ComponentModel;
using System.Xml.Serialization;
using org.SpocWeb.root.Data.xmls.MathML.Attributes;

namespace org.SpocWeb.root.Data.xmls.MathML.Elements;

/// <summary> A Fraction is a binary Operator ans has exactly 2 Children: Numerator and Denominator </summary>
/// <remarks>
/// By setting the <see cref="LineThickness"/> attribute to 0, 
/// the mfrac element can also be used for binomial coefficients. 
/// </remarks>
[XmlRoot("mfrac")]
public class MFraction : MElement {

	public MFraction() { }

	public MFraction(bool bevelled, MHorizontalAlign denominatorAlignment
		, MHorizontalAlign numeratorAlignment) {
		Bevelled = bevelled;
		DenominatorAlignment = denominatorAlignment;
		NumeratorAlignment = numeratorAlignment;
	}

	[DefaultValue(false)]
	public bool Bevelled { get; set; }

	[DefaultValue(1.0)]
	public double LineThickness { get; set; }// = 1;

	[XmlAttribute("numalign")]
	[DefaultValue(MHorizontalAlign.Center)]
	public MHorizontalAlign NumeratorAlignment { get; set; }

	[XmlAttribute("denomalign")]
	[DefaultValue(MHorizontalAlign.Center)]
	public MHorizontalAlign DenominatorAlignment { get; set; }

	public override sbyte Arity => 2;

	public override TextWriter AppendTo(TextWriter sb, bool toAscii) {
		Numerator.AppendTo(sb, toAscii).Write('/');
		return Denominator.AppendTo(sb, toAscii);
	}

	public AMathMlNode Numerator => Children[0];
	public AMathMlNode Denominator => Children[1];

}