using System.Xml.Serialization;

namespace org.SpocWeb.root.Data.xmls.MathML.Elements {

	public class MPhantom : MElement {
		[XmlIgnore] public override sbyte Arity => ImpliedMRowArity;
	}

}