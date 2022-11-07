using System.Xml.Serialization;

namespace org.SpocWeb.root.Data.xmls.MathML.Elements;

[XmlRoot("mlongdiv")]
public class MLongDivision : MElement {
	//[XmlIgnore] public override Type ChildType => typeof(MStackExpression);
	[XmlIgnore] public override sbyte Arity => sbyte.MaxValue;
}