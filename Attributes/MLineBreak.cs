using System.ComponentModel;

namespace org.SpocWeb.root.Data.xmls.MathML.Attributes;

[DefaultValue(Auto)]
public enum MLineBreak : sbyte {
	Auto = 1,

	NewLine,

	NoBreak,

	GoodBreak,

	BadBreak 
}