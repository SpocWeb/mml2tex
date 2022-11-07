using System.Xml.Serialization;

namespace org.SpocWeb.root.Data.xmls.MathML.Elements;

/// <summary> Contents should be displayed as identifiers.  </summary>
/// <remarks>
/// Single character identifiers like 'x' and 'h' should appear in italics, 
/// while multi-character identifiers like 'sin' and 'log' should be in an upright font.
/// Attributes include font properties like fontweight, fontfamily and fontstyle 
/// as well as general properties like color.
/// </remarks>
[XmlRoot("mi")]
public class MIdentifier : MMultiElement { //MElement

}