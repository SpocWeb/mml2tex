namespace org.SpocWeb.root.Data.xmls.MathML.Attributes;

public static class XMathVariant {

	public const string STR_OPEN = ""; //"{";

	public static string Prefix(this MMathVariant mathVariant) {
		switch (mathVariant) {
			case MMathVariant.Normal: break;
			case MMathVariant.Bold: return "bb"+STR_OPEN;
			case MMathVariant.Italic: return "it"+STR_OPEN;
			case MMathVariant.Bold_Italic: return "bb"+STR_OPEN+"it"+STR_OPEN;
			case MMathVariant.Double_Struck: return "bbb"+STR_OPEN;
			case MMathVariant.Bold_Fraktur: return "bb"+STR_OPEN+"fr"+STR_OPEN;
			case MMathVariant.Script: return "cc"+STR_OPEN;
			case MMathVariant.Bold_Script: return "bb"+STR_OPEN+"cc"+STR_OPEN;
			case MMathVariant.Fraktur: return "fr"+STR_OPEN;
			case MMathVariant.Sans_Serif: return "sf"+STR_OPEN;
			case MMathVariant.Bold_Sans_Serif: return "bb"+STR_OPEN+"sf"+STR_OPEN;
			case MMathVariant.Sans_Serif_Italic: return "it"+STR_OPEN+"sf"+STR_OPEN;
			case MMathVariant.Sans_Serif_Bold_Italic: return "bb"+STR_OPEN+"it"+STR_OPEN+"sf"+STR_OPEN;
			case MMathVariant.Monospace: return "tt"+STR_OPEN;
			case MMathVariant.Initial: break;
			case MMathVariant.Tailed: break;
			case MMathVariant.Looped: break;
			case MMathVariant.Stretched: break;
			default: throw new ArgumentOutOfRangeException();
		}
		return "";
	}

}

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum MMathVariant : sbyte {

	Normal = 1,

	Bold,

	Italic,

	Bold_Italic,

	Double_Struck,

	Bold_Fraktur,

	Script,

	Bold_Script,

	Fraktur,

	Sans_Serif,

	Bold_Sans_Serif,

	Sans_Serif_Italic,

	Sans_Serif_Bold_Italic,

	Monospace,

	Initial,

	Tailed,

	Looped,

	Stretched 
}