using System.Xml;

namespace org.SpocWeb.root.Data.xmls.MathML {

	public class MathMlTextNode : AMathMlNode {

		public MathMlTextNode(string content) => Content = content;

		public string Content { get; set; }

		public override void WriteXml(XmlWriter writer) {
			writer.WriteString(Content);
		}

		public override void ReadXml(XmlReader reader) {
			Content = reader.ReadString();
		}

		internal override AsciiMath.Parser.AmSymbol GetSymbol() 
			=> AsciiMath.Parser.ASCII_NAMES_BY_CHAR.TryGetValue(Content, out var symbol) ? symbol : null;

		public override TextWriter AppendTo(TextWriter sb, bool toAscii) {
			sb.Write(toAscii && AsciiMath.Parser.ASCII_NAMES_BY_CHAR.TryGetValue(Content, out var symbol) 
				? symbol.Input : Content);
			return sb;
		}

	}
}