using System.Globalization;

namespace org.SpocWeb.root.Data.xmls.MathML {
	
	/// Canonical: https://www.w3.org/TR/2008/REC-xml-c14n11-20080502/ 
	/// Normal: https://www.w3.org/2008/xmlsec/Drafts/xml-norm/Overview.html 
	/// Its intent is different from that of Canonicalized XML 1.1: 
	/// it exists primarily to assist clients of XML parser APIs such as SAX [SAX] 
	/// to ensure that they are provided XML data in a predefined representation, 
	/// whether as events or DOM nodes. 
	/// <see cref="TextXmlWriter"/> expands on this 
	public class XmlStringWriter : StringWriter {

		public static string ValueTrue = "true";
		public static string ValueFalse = "false";

		public XmlStringWriter(IFormatProvider? formatter = null) 
			: base(formatter ?? CultureInfo.InvariantCulture){}

		public int Length {
			get => GetStringBuilder().Length; 
			set => GetStringBuilder().Length = value; 
		}

		public override void Write(bool value) {
			Write(value ? ValueTrue : ValueFalse);
		}

	}
}