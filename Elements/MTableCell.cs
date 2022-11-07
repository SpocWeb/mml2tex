using System.ComponentModel;
using System.Xml.Serialization;
using org.SpocWeb.root.Data.xmls.MathML.Attributes;

namespace org.SpocWeb.root.Data.xmls.MathML.Elements;

/// <summary>
/// make one cell span several rows or columns.
/// </summary>
[XmlRoot("mtd")]
public class MTableCell : MMultiElement {

	[DefaultValue(1)]
	public int RowSpan { get; set; }

	[DefaultValue(1)]
	public int ColumnSpan { get; set; }

	[DefaultValue(MVerticalAlign.BaseLine)]
	public MVerticalAlign RowAlign { get; set; }

	[DefaultValue(MHorizontalAlign.Center)]
	public MHorizontalAlign ColumnAlign { get; set; }

	[XmlIgnore] public override sbyte Arity => ImpliedMRowArity;
}