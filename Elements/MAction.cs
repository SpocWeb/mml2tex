using System.Xml.Serialization;

namespace org.SpocWeb.root.Data.xmls.MathML.Elements {

	public class MAction : MElement {

		public string ActionType { get; set; }

		/// <summary> Exactly 1 Text Node </summary>
		[XmlIgnore] public override sbyte Arity => sbyte.MaxValue; 

	}
}