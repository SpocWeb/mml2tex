using org.SpocWeb.root.Data.xmls.MathML.Attributes;

namespace org.SpocWeb.root.Data.xmls.MathML.Elements;

/// <summary> Can contain <see cref="MathMlTextNode"/> and <see cref="MGlyph"/> Nodes </summary>
public class MText : MMultiElement {

	//[DefaultValue(MMathVariant.Normal)]
	public MMeasuredLength MathSize { get; set; }

	public override TextWriter AppendTo(TextWriter textWriter, bool toAscii) {
		textWriter.Write('"');
		var sb = new StringWriter();
		base.AppendTo(sb, toAscii);
		textWriter.Write(sb.GetStringBuilder().Replace("\"", "\"\""));
		textWriter.Write('"');
		return textWriter; //"∪";
	}

}