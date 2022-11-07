using System.Globalization;

namespace org.SpocWeb.root.Data.xmls.MathML.Attributes;

public class MMeasuredLength : ITextWriteAble {

	public MMeasuredLength(double quantity, MMeasuredLengthUnit units) {
		Quantity = quantity;
		Units = units;
	}

	public double Quantity { get; }

	public MMeasuredLengthUnit Units { get; }

	public long WriteTo(TextWriter writer, long lengthLeft) {
		writer.Write(Quantity.ToString(CultureInfo.InvariantCulture));
		writer.WriteEnum(Units);
		return lengthLeft;
	}
}