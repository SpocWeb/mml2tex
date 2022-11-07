using System.ComponentModel;
using System.Xml.Serialization;
using org.SpocWeb.root.Data.xmls.MathML.Attributes;

namespace org.SpocWeb.root.Data.xmls.MathML.Elements;

/// <summary> <see cref="MTable"/> contains any number of <see cref="MTableRow"/> table row elements.  </summary>
/// <remarks>
/// A Table consists of (,,,) Rows enclosed by Braces on both sides. 
/// Braces are put into the same row in pre- and post- Position using {mo} Elements. 
/// Braces can be made invisible using {: or :}. 
/// rowalign and columnalign attributes can be used to determine 
/// how the entries in rows and columns should be aligned, e.g. "center", "left", "top", etc. 
/// The <see cref="RowLines"/>, <see cref="ColumnLines"/> and <see cref="Frame"/> attributes can be used to draw separator lines. 
/// <see cref="RowSpacing"/>, <see cref="ColumnSpacing"/>, <see cref="EqualRows"/>, and <see cref="EqualColumns"/> 
/// determine the spacing between rows and columns.
/// </remarks>
public class MTable : MMultiElement {

	public MTable() {
		_Open = "{:"; //needed to group the Table...
		_Separators = ",";
		_Close = ":}"; //the actual Borders omitted or provided using <mo>{</mo>
	}

	/// <summary> vertikale Ausrichtung der Tabelle im Bezug auf die Umgebung </summary>
	/// <remarks>
	/// * axis (Vorgabewert): Die vertikale Mitte (das Minuszeichen) liegt auf der Höhe der Achse der Umgebung.
	/// * baseline: Die vertikele Mitte der Tabelle liegt auf der Höhe der Grundlinie der Umgebung.
	/// * bottom: Der untere Rand der Tabelle liegt auf der Höhe der Grundlinie der Umgebung.
	/// * center: gleichbedeutend mit baseline.
	/// * top: Der obere Rand der Tabelle liegt auf der Höhe der Grundlinie der Umgebung.
	/// Darüberhinaus können die Werte des Attributes von align auch mit einer Zeilennummer enden, z.B. align="center 3". 
	/// Dadurch wird die angegebene Tabellenzeile und nicht die Tabelle als ganzes an der Umgebung ausgerichtet. 
	/// Bei Angabe einer negativen, ganzen Zahl werden die Tabellenzeilen von unten nach oben gezählt.
	/// </remarks>
	[DefaultValue(MVerticalAlign.Axis)]
	public MVerticalAlign Align { get; set; }

	#region Vertical Row Layout

	/// <summary> vertikale Ausrichtung des Inhalts der Tabellenzellen. </summary>
	/// <remarks>
	/// Die Angabe mehrerer Werte ist erlaubt; der letzte Wert wird auf die nachfolgenden Spalten angewandt.
	/// </remarks>
	[DefaultValue(MVerticalAlign.BaseLine)]
	public List<MVerticalAlign> RowAlign { get; set; }

	/// <summary> Umrandung der Zeilen: none (default), solid und dashed. </summary>
	/// <remarks>
	/// Die Angabe mehrerer Werte ist erlaubt; der letzte Wert wird auf die nachfolgenden Zeilen angewandt.
	/// </remarks>
	[DefaultValue(MTableLineStyle.None)]
	public List<MTableLineStyle> RowLines { get; set; }

	public MMeasuredLength RowSpacing { get; set; }

	#endregion Vertical Row Layout

	#region Horizontal Column Layout

	/// <summary>horizontale Ausrichtung des Inhalts der Tabellenzellen: left, center (default) und right. </summary>
	/// <remarks>
	/// Die Angabe mehrerer Werte ist erlaubt; der letzte Wert wird auf die nachfolgenden Spalten angewandt.
	/// </remarks>
	[DefaultValue(MHorizontalAlign.Center)]
	public List<MHorizontalAlign> ColumnAlign { get; set; }

	/// <summary> Umrandung der Spalten: none (default), solid und dashed. </summary>
	/// <remarks>
	/// Die Angabe mehrerer Werte ist erlaubt; der letzte Wert wird auf die nachfolgenden Spalten angewandt.
	/// </remarks>
	[DefaultValue(MTableLineStyle.None)]
	public List<MTableLineStyle> ColumnLines { get; set; }

	public MMeasuredLength ColumnSpacing { get; set; }

	public MMeasuredLength ColumnWidth { get; set; }

	#endregion Horizontal Column Layout

	[DefaultValue(true)]
	public bool DisplayStyle { get; set; }

	[DefaultValue(false)]
	public bool EqualColumns { get; set; }

	[DefaultValue(false)]
	public bool EqualRows { get; set; }

	[DefaultValue(MTableLineStyle.None)]
	public MTableLineStyle Frame { get; set; }

	public MMeasuredLength FrameSpacing { get; set; }

	public MMeasuredLength MinLabelSpacing { get; set; }

	[DefaultValue(MLabeledTableRowPlacement.Right)]
	public MLabeledTableRowPlacement Side { get; set; }

	public MMeasuredLength Width { get; set; }

	[XmlIgnore] public override sbyte Arity => sbyte.MaxValue;

	[XmlIgnore] public override Type ChildType => typeof(MTableRow);

}