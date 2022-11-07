using System.ComponentModel;

namespace org.SpocWeb.root.Data.xmls.MathML.Elements;

/// <summary> like an <see cref="MRow"/>, except that it displays enclosed in parentheses. </summary>
/// <remarks>
/// Using attributes <see cref="Open"/>, <see cref="Close"/> and <see cref="Separators"/>, 
/// one can set the beginning and ending delimiter character, 
/// as well as multiple separator characters like commas.
/// </remarks>
public class MFenced : MMultiElement {

	public MFenced() { }

	public MFenced(string open, string close) : this(open, ",", close) { }

	public MFenced(string open, string separators, string close) {
		Open = open;
		Separators = separators;
		Close = close;
	}

	[DefaultValue("(")]
	public string Open {
		get => _Open;
		set => _Open = value;
	}

	/// <summary> Sequence of zero or more characters to be used for different separators, optionally divided by white space, which is ignored. </summary>
	/// <remarks>
	/// By specifying more than one character, it is possible to set different separators
	/// for each argument in the expression. 
	/// If there are too many separators, all excess is ignored. 
	/// If there are too few separators in the expression, the last specified separator is repeated. 
	/// </remarks>
	[DefaultValue(",")]
	public string Separators {
		get => _Separators;
		set => _Separators = value;
	}

	[DefaultValue(")")]
	public string Close {
		get => _Close;
		set => _Close = value;
	}

}