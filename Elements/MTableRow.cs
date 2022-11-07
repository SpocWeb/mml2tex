using System.ComponentModel;
using System.Xml.Serialization;
using org.SpocWeb.root.Data.xmls.MathML.Attributes;

namespace org.SpocWeb.root.Data.xmls.MathML.Elements;

/// <summary> <see cref="MTableRow"/> elements contain any number of <see cref="MTableCell"/> table data cells. </summary>
[XmlRoot("mtr")]
public class MTableRow : MMultiElement {

	public MTableRow() {
		_Open = "(";
		_Separators = ",";
		_Close = ")";
	}

	[DefaultValue(MVerticalAlign.BaseLine)]
	public MVerticalAlign RowAlign { get; set; }

	[DefaultValue(MHorizontalAlign.Center)]
	public MHorizontalAlign ColumnAlign { get; set; }

	/// <summary> All Children are <see cref="MTableCell"/>s! </summary>
	[XmlIgnore] public override sbyte Arity => sbyte.MaxValue;

	[XmlIgnore] public override Type ChildType => typeof(MTableCell);

}