using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace org.SpocWeb.root.Data.xmls.MathML {

	/// <summary> Common Base for <see cref="MathMlTextNode"/> and <see cref="MElement"/> </summary>
	/// <remarks>introduced to support <see cref="XmlSerializer"/></remarks>
	public abstract class AMathMlNode : IXmlSerializable { //alles selbst machen? übersichtlicher als überall Attribute ranzuklatschen {

		public const string NameSpace = "http://www.w3.org/1998/Math/MathML";

		[XmlIgnore] 
		//[IgnoreDataMember]
		public MElement Parent { get; set; }

		public void Serialize(TextWriter writer) => this.WriteTo(writer);

		public XmlSchema GetSchema() => null;
		public abstract void WriteXml(XmlWriter writer); // => this.WriteTo(writer);
		public abstract void ReadXml(XmlReader reader);
		internal abstract AsciiMath.Parser.AmSymbol GetSymbol();

		public virtual TextWriter AppendTo(TextWriter textWriter, bool toAscii) {
			textWriter.Write(GetType().Name);
			return textWriter;
		}

		public sealed override string ToString() => ToString(false);
		public string ToString(bool toAscii) {
			var sb = new StringWriter();
			AppendTo(sb, toAscii);
			return sb.ToString(); 
		}

	}

}