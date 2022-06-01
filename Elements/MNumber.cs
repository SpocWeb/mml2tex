using System.ComponentModel;
using System.Xml.Serialization;

namespace org.SpocWeb.root.Data.xmls.MathML.Elements {

	/// <summary> contents should be rendered as {b numbers}, which generally means in an upright font. </summary>
	[XmlRoot("mn")]
	public class MNumber : MMultiElement { //MElement AMathMlNode {

		//public double Real; 
		//public double Imag; 

		/// <summary> The Base of the Number Representation </summary>
		[DefaultValue(10)]
		public sbyte Base { get; set; }

	}
}