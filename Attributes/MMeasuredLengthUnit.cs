using System.Xml.Serialization;

namespace org.SpocWeb.root.Data.xmls.MathML.Attributes {

	public enum MMeasuredLengthUnit : byte {

		Em = 1,

		Ex,

		[XmlEnum("px")]
		Pixels,

		[XmlEnum("in")]
		Inches,

		[XmlEnum("cm")]
		CentiMeters,

		[XmlEnum("mm")]
		MilliMeters,

		[XmlEnum("pt")]
		Points,

		[XmlEnum("pc")]
		Picas,

		[XmlEnum("%")]
		Percent
	}
}