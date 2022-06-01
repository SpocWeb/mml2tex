using System.Xml.Serialization;

namespace org.SpocWeb.root.Data.xmls.MathML.Attributes {

	[Flags]
	public enum MEncloseNotation {

		[XmlEnum("longdiv")]
		LongDivision = 1 << 0,

		UpDiagonalStrike = 1 << 1,

		DownDiagonalStrike = 1 << 2,

		VerticalStrike = 1 << 3,

		HorizontalStrike = 1 << 4,

		Actuarial = 1 << 5,

		Radical = 1 << 6,

		Box = 1 << 7,

		RoundedBox = 1 << 8,

		Circle = 1 << 9,

		Left = 1 << 10,

		Right = 1 << 11,

		Top = 1 << 12,

		Bottom = 1 << 13,

		Madruwb = 1 << 14,

		UpDiagonalArrow = 1 << 15,

		/// <summary> used in circuit analysis </summary>
		PhasOrAngle = 1 << 16
	}
}