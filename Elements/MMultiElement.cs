using System.ComponentModel;
using System.Xml.Serialization;
using org.SpocWeb.root.Data.xmls.MathML.Attributes;

namespace org.SpocWeb.root.Data.xmls.MathML.Elements;

/// <summary> Can contain <see cref="MathMlTextNode"/> and <see cref="MGlyph"/> Nodes </summary>
public abstract class MMultiElement : MElement {

	[XmlAttribute("dir")]
	[DefaultValue(MTextDirection.LtR)]
	public MTextDirection TextDirection { get; set; }

	/// <summary> But can contain mixed Content with <see cref="MGlyph"/> and see cref="MAlignMark"/> </summary>
	[XmlIgnore] public override sbyte Arity => sbyte.MaxValue;
}