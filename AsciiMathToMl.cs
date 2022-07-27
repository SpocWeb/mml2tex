using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

//#pragma warning disable 169

namespace org.SpocWeb.root.Data.xmls.MathML {

	/// <summary> convert ASCII math notation and (some) LaTeX to Presentation MathML. </summary>
	/// <example>
	/// <see cref="ParseAsciiMath"/> converts an asciiMath String to MathML.
	/// see AsciiMathToMlTests to save a Reference to NUnit.
	///
	/// This Implementation uses explicit <see cref="M_FENCED"/> Elements
	/// instead of unstructured <see cref="M_O"/> Elements with bracket Characters.
	/// This allows better post-processing to e.g. eliminate redundant Braces. 
	///
	/// More examples here: https://www.intmath.com
	/// Good MathMl Rendering: https://www.tutorialspoint.com/online_mathml_editor.php 
	/// <a href="https://developer.mozilla.org/en-US/docs/Web/MathML"/>
	/// <a href="http://wjagray.co.uk/maths/ASCIIMathTutorial.html"/>
	/// https://www.tutorialspoint.com/mathml
	/// In Presentational MathML the Function Names are only Symbols enclosed in {mi/} Elements, 
	/// but in semantic Content MathML ALL Names must be lower-case! 
	/// https://www.w3.org/TR/MathML3/ 
	/// Renders a Table only when the Number of Elements in all Rows match! 
	/// Column Borders can be introduced by inserting | between Columns in the first Row 
	/// 
	/// Parsing ASCII math expressions with the following grammar
	/// v ::= [^\W_0-9]+ | greek letters | numbers | other constant symbols
	/// u ::= sqrt | text | bb | other unary symbols for font commands
	/// b ::= frac | root | stackRel         binary symbols => AmGetSymbol()
	/// l ::= ( | [ | { | (: | {: &lt;&lt;   left  brackets ([{⟨ and invisible
	/// r ::= ) | ] | } | :) | :} >>         right brackets )]}⟩ and invisible
	/// S ::= v | lEr | uS | bSS |v!         Simple expression => AmParseS()
	/// I ::= S_S | S^S | S_S^S | S          Intermediate expression => AmParseI()
	/// E ::= IE | I/I                       Expression => AmParseExpr
	/// Each terminal symbol is translated into a corresponding mathMl node.
	/// 
	/// Operators are processed LtR (left-to-right) 
	/// unless their Precedence Levels differ:
	/// 0 Brackets: ({[]})
	/// 1 Factorial: !
	/// 2 Multiplication, Division: */
	/// 3 Addition, Subtraction: +-
	/// </example>
	public static class AsciiMath {
		/// <inheritdoc cref="Parser.ParseAsciiMath"/>
		public static XmlNode Parse(string asciiMath) => new Parser().ParseAsciiMath(asciiMath);

		public static void Main(string[] args) {
			foreach(var str in args) {
				Console.WriteLine(Parse(str).OuterXml);
				Console.WriteLine();
			}
		}

	public class Parser {

		#region static

		#region MathML Tag Names

		public const string M_MATH = "math";

		/// <summary> used change the style of its children. </summary>
		/// <remarks>
		/// It accepts all attributes of all MathML presentation elements with some exceptions and additional attributes.
		/// https://developer.mozilla.org/en-US/docs/Web/MathML/Element/mstyle
		/// </remarks>
		public const string M_STYLE = "mstyle";

		/// <summary> represents an operator in a broad sense.  </summary>
		/// <remarks>
		/// Besides operators in strict mathematical meaning, this element also includes "operators" like
		/// parentheses, separators like comma and semicolon, or "absolute value" bars.
		/// https://developer.mozilla.org/en-US/docs/Web/MathML/Element/mo
		/// </remarks>
		public const string M_O = "mo";

		/// <summary> content should be rendered as an identifier such as function names, variables or symbolic constants </summary>
		/// <remarks>
		/// https://developer.mozilla.org/en-US/docs/Web/MathML/Element/mi
		/// </remarks>
		public const string M_I = "mi";

		/// <summary> numeric literal which is normally a sequence of digits with a possible separator (a dot or a comma). </summary>
		/// <remarks>
		/// However,  it is also allowed to have arbitrary text in it which is actually a numeric quantity, for example "eleven".
		/// https://developer.mozilla.org/en-US/docs/Web/MathML/Element/mn
		/// </remarks>
		public const string M_N = "mn";

		/// <summary> used to render arbitrary HTML text with no notational meaning, such as comments or annotations. </summary>
		/// <remarks>
		/// use <see cref="M_I"/> or <see cref="M_O"/> for notational meaning
		/// </remarks>
		public const string M_TEXT = "mtext";

		/// <summary> used to display fractions </summary>
		/// <remarks>
		/// https://developer.mozilla.org/en-US/docs/Web/MathML/Element/mfrac 
		/// </remarks>
		public const string M_FRAC = "mfrac";

		/// <summary> Diagonal Strike-Through resp. cancelling of Terms cancel(x) </summary>
		public const string STR_CANCEL = "cancel";

		/// <summary> display square roots; use <see cref="M_ROOT"/> for general Roots </summary>
		/// <remarks>
		/// https://developer.mozilla.org/en-US/docs/Web/MathML/Element/msqrt
		/// </remarks>
		public const string M_SQRT = "msqrt";

		/// <summary> used to display roots with an explicit index </summary>
		/// <remarks>
		/// Can use <see cref="M_SQRT"/> for binary Roots.
		/// https://developer.mozilla.org/en-US/docs/Web/MathML/Element/mroot
		/// </remarks>
		public const string M_ROOT = "mroot";

		/// <summary> renders as a horizontal row of grouped sub-expressions; usually contain one or more operators with their respective operands  </summary>
		/// <remarks>
		/// https://developer.mozilla.org/en-US/docs/Web/MathML/Element/mrow
		/// </remarks>
		public const string M_ROW = "mrow";

		/// <summary> Attributes to add custom @open='(' and @close=')' parentheses (such as brackets/braces) and @separators=',' character List (comma is the default) to an expression </summary>
		/// <remarks>
		/// https://developer.mozilla.org/en-US/docs/Web/MathML/Element/mfenced
		/// </remarks>
		public const string M_FENCED = "mfenced";

		/// <summary> renders its content inside an enclosing notation specified by the @notation attribute </summary>
		public const string M_ENCLOSE = "menclose";

		/// <summary>
		/// display a blank space with @width=, @depth= @height=
		/// </summary>
		public const string M_SPACE = "mspace";

		/// <summary> used to attach both a subscript and a superscript, together, to an expression. </summary>
		/// <remarks>
		/// It uses the following syntax: <msubsup> base subscript superscript </msubsup>.
		/// https://developer.mozilla.org/en-US/docs/Web/MathML/Element/msubsup
		/// </remarks>
		public const string M_SUB_SUP = "msubsup";

		/// <summary> used to attach a superscript to an expression; uses the following syntax: <msup> base superscript </msup>. </summary>
		/// <remarks>
		/// https://developer.mozilla.org/en-US/docs/Web/MathML/Element/msup
		/// </remarks>
		public const string M_SUP = "msup";

		/// <summary> used to attach a subscript to an expression; uses the following syntax: <msub> base subscript </msub>. </summary>
		/// <remarks>
		/// https://developer.mozilla.org/en-US/docs/Web/MathML/Element/msub
		/// </remarks>
		public const string M_SUB = "msub";

		public const string CHR_OVER = "^";
		public const string CHR_UNDER = "_";

		/// <summary> used to attach accents or limits both under and over an expression. </summary>
		/// <remarks>
		/// It uses the following syntax: <munderover> base underScript overScript </munderover>
		/// https://developer.mozilla.org/en-US/docs/Web/MathML/Element/munderover
		/// </remarks>
		public const string M_UNDER_OVER = "munderover";

		/// <summary> used to attach an accent or a limit UNDER an expression; uses syntax: <munder> base underScript </munder> </summary>
		/// <remarks>
		/// https://developer.mozilla.org/en-US/docs/Web/MathML/Element/munder
		/// </remarks>
		public const string M_UNDER = "munder";

		/// <summary> used to attach an accent or a limit over an expression. Use the following syntax: <mover> base overScript </mover> </summary>
		/// <remarks>
		/// https://developer.mozilla.org/en-US/docs/Web/MathML/Element/mover
		/// </remarks>
		public const string M_OVER = "mover";

		/// <summary> Used for Text </summary>
		public const string M_BOX = "mbox";

		/// <summary> Table containing only <see cref="M_TR"/> Elements</summary>
		public const string M_TABLE = "mtable";
		/// <summary> Table Row; a row in a table or a matrix. It may only appear in a <see cref="M_TABLE"/> element. </summary>
		public const string M_TR = "mtr";
		/// <summary> Table Data; a cell in a table or a matrix. It may only appear in a <see cref="M_TR"/> element. </summary>
		public const string M_TD = "mtd";

		#endregion MathML Tag Names

		#region TeX Commands

		public const string TEX_OVER_SET = "overSet";
		public const string TEX_UNDER_SET = "underSet";

		#endregion TeX Commands

		#region MathML Attributes

		public const string ATTR_COLUMN_LINES = "columnlines";
		public const string ATTRIB_COLUMN_ALIGN = "columnalign";
		public const string ATTRIB_COLUMN_ALIGN_VALUE = "left";

		public const string ATTR_WIDTH = "width";
		public const string ATTR_WIDTH_VALUE = "1ex";

		public const string ATTRIB_MATH_COLOR = "mathcolor";
		public const string ATTRIB_FONT_SIZE = "fontsize";
		public const string ATTRIB_MATH_SIZE = "mathsize";
		public const string ATTRIB_FONT_FAMILY = "fontfamily";
		public const string ATTRIB_MATH_VARIANT = "mathvariant";
		public const string ATTRIB_DISPLAY_STYLE = "displaystyle";
		public const string ATTRIB_TITLE = "title";

		public const string ATTRIB_NOTATION = "notation";
		public const string ATTRIB_NOTATION_UP_DIAGONAL_STRIKE = "updiagonalstrike";
		#endregion MathML Attributes

		public const string STR_COLOR = "color";
		const string STR_VEC = "vec";
		const string STR_SQRT = "sqrt";
		const string STR_ROOT = "root";
		const string STR_FRAC = "frac";
		const string STR_CLASS = "class";
		const string STR_ID = "id";

		/// <summary> false to return to legacy phi/varPhi mapping </summary>
		public static bool FixPhi = true; 

		/// <summary> TeX Command to stack Elements above each other, similar to <see cref="M_OVER"/> </summary>
		const string STR_STACK_REL = "stackrel";

		const string M_MROW = "M:MROW";
		const string STR_V_BAR = "\u2223";
		const string COL_LINES_NONE = "none";

		class ParseResult {
			public ParseResult(XmlNode node, string rest) {
				Node = node;
				Rest = rest;
			}
			public XmlNode Node;
			public readonly string Rest;

			public override string ToString() => Node.InnerText + " Rest: " + Rest;
		}

		static readonly XmlDocument _Document = new();

		static XmlElement CreateElementXhtml(string t) => _Document.CreateElement(t, "http://www.w3.org/1999/xhtml");

		//XmlElement AmCreateElementMathMl(string t) => _Document.CreateElement(t, _AmMathMl);

		/// <summary> MathML Tags </summary>
		public enum Tag {
			/// <summary> Number, 0ary </summary>
			N,
			/// <summary> Identifier, 0ary </summary>
			I,
			/// <summary> Operator, binary Infix Position </summary>
			O,
			/// <summary> Table </summary>
			Table,
			/// <summary> Table Row </summary>
			Tr,
			/// <summary> Table Data / Cell</summary>
			Td,
			/// <summary> Grouping Element, substituted by <see cref="Fenced"/>  </summary>
			Row,
			/// <summary> Literal Text in Quotes </summary>
			Text,
			/// <summary> Square-Root </summary>
			SqRt,
			/// <summary> General Root </summary>
			Root,
			/// <summary> Fenced, Left and Right Bracket </summary>
			Fenced,
			/// <summary> Fraction, Binary </summary>
			Frac,
			/// <summary> Sub-Position, Binary </summary>
			Sub,
			/// <summary> Super-Position, Binary </summary>
			Sup,
			/// <summary> Position below and above, Ternary </summary>
			UnderOver,
			/// <summary> both Sub-Sup-Position, Ternary </summary>
			SubSup,
			/// <summary> Over-Position, Binary </summary>
			Over,
			/// <summary> Under-Position, Binary </summary>
			Under,
			/// <summary> Empty styling Node </summary>
			Style,
			/// <summary> Arbitrary 2D Space </summary>
			Space,
			/// <summary> Root Element </summary>
			Math,
			Enclose,
		}

		/// <summary> Syntactical Position </summary>
		internal enum Token {
			/// <summary> No Argument; 0ary </summary>
			Const = 1,
			/// <summary> One Argument </summary>
			Unary,
			/// <summary> Prefix Operator; Expects 2 Arguments sep by ()() or by (&amp;) </summary>
			Binary,
			/// <summary> Infix Operator; Expects 2 Arguments sep by itself </summary>
			Infix,
			LeftBracket,
			RightBracket,
			LeftRightBracket,

			/// <summary> Adds an <see cref="Elements.MSpace"/> to the left and the right </summary>
			Space,

			UnderOver,
			Definition,
			/// <summary> Verbatim Text </summary>
			Text,
			Big,
			Long,
			/// <summary> Indicates the Item should stretch to the Contents </summary>
			Stretchy,
			Matrix,
			UnaryUnderOver
		} // token types

		/// <summary> Grouping of Symbols for Documentation </summary>
		internal enum Typ {
			Greek,
			Operator,
			Relation,
			Logic,
			Bracket,
			Symbol,
			Function,
			Arrow,
			Font,
			Adorn,
			Parsed
		}

		static XmlElement CreateMmlNode(Tag t, XmlNode? child = null) {
			var node = _Document.CreateElement('m' + t.ToString().ToLower(), MathMlNs);
			if (child is not null) {
				node.AppendChild(child);
			}
			return node;
		}
		static XmlElement CreateSentinel() {
			var sentinel = CreateMmlNode(Tag.O);
			sentinel.InnerText = ",";
			return sentinel;
		}

		#region Alphabet in different Fonts

		static readonly string[] _AM_CAL = {
			"\uD835\uDC9C", "\u212C", "\uD835\uDC9E", "\uD835\uDC9F", "\u2130", "\u2131", "\uD835\uDCA2", "\u210B", "\u2110"
			, "\uD835\uDCA5", "\uD835\uDCA6", "\u2112", "\u2133", "\uD835\uDCA9", "\uD835\uDCAA", "\uD835\uDCAB", "\uD835\uDCAC"
			, "\u211B", "\uD835\uDCAE", "\uD835\uDCAF", "\uD835\uDCB0", "\uD835\uDCB1", "\uD835\uDCB2", "\uD835\uDCB3"
			, "\uD835\uDCB4", "\uD835\uDCB5", "\uD835\uDCB6", "\uD835\uDCB7", "\uD835\uDCB8", "\uD835\uDCB9", "\u212F"
			, "\uD835\uDCBB", "\u210A", "\uD835\uDCBD", "\uD835\uDCBE", "\uD835\uDCBF", "\uD835\uDCC0", "\uD835\uDCC1"
			, "\uD835\uDCC2", "\uD835\uDCC3", "\u2134", "\uD835\uDCC5", "\uD835\uDCC6", "\uD835\uDCC7", "\uD835\uDCC8"
			, "\uD835\uDCC9", "\uD835\uDCCA", "\uD835\uDCCB", "\uD835\uDCCC", "\uD835\uDCCD", "\uD835\uDCCE", "\uD835\uDCCF"
		};

		static readonly string[] _AM_FRK = {
			"\uD835\uDD04", "\uD835\uDD05", "\u212D", "\uD835\uDD07", "\uD835\uDD08", "\uD835\uDD09", "\uD835\uDD0A", "\u210C"
			, "\u2111", "\uD835\uDD0D", "\uD835\uDD0E", "\uD835\uDD0F", "\uD835\uDD10", "\uD835\uDD11", "\uD835\uDD12"
			, "\uD835\uDD13", "\uD835\uDD14", "\u211C", "\uD835\uDD16", "\uD835\uDD17", "\uD835\uDD18", "\uD835\uDD19"
			, "\uD835\uDD1A", "\uD835\uDD1B", "\uD835\uDD1C", "\u2128", "\uD835\uDD1E", "\uD835\uDD1F", "\uD835\uDD20"
			, "\uD835\uDD21", "\uD835\uDD22", "\uD835\uDD23", "\uD835\uDD24", "\uD835\uDD25", "\uD835\uDD26", "\uD835\uDD27"
			, "\uD835\uDD28", "\uD835\uDD29", "\uD835\uDD2A", "\uD835\uDD2B", "\uD835\uDD2C", "\uD835\uDD2D", "\uD835\uDD2E"
			, "\uD835\uDD2F", "\uD835\uDD30", "\uD835\uDD31", "\uD835\uDD32", "\uD835\uDD33", "\uD835\uDD34", "\uD835\uDD35"
			, "\uD835\uDD36", "\uD835\uDD37"
		};

		static readonly string[] _AM_BBB = {
			"\uD835\uDD38", "\uD835\uDD39", "\u2102", "\uD835\uDD3B", "\uD835\uDD3C", "\uD835\uDD3D", "\uD835\uDD3E", "\u210D"
			, "\uD835\uDD40", "\uD835\uDD41", "\uD835\uDD42", "\uD835\uDD43", "\uD835\uDD44", "\u2115", "\uD835\uDD46", "\u2119"
			, "\u211A", "\u211D", "\uD835\uDD4A", "\uD835\uDD4B", "\uD835\uDD4C", "\uD835\uDD4D", "\uD835\uDD4E", "\uD835\uDD4F"
			, "\uD835\uDD50", "\u2124", "\uD835\uDD52", "\uD835\uDD53", "\uD835\uDD54", "\uD835\uDD55", "\uD835\uDD56"
			, "\uD835\uDD57", "\uD835\uDD58", "\uD835\uDD59", "\uD835\uDD5A", "\uD835\uDD5B", "\uD835\uDD5C", "\uD835\uDD5D"
			, "\uD835\uDD5E", "\uD835\uDD5F", "\uD835\uDD60", "\uD835\uDD61", "\uD835\uDD62", "\uD835\uDD63", "\uD835\uDD64"
			, "\uD835\uDD65", "\uD835\uDD66", "\uD835\uDD67", "\uD835\uDD68", "\uD835\uDD69", "\uD835\uDD6A", "\uD835\uDD6B"
		};

		static readonly char[] _AM_CAL_BASE_PLANE= {'\uEF35','\u212C','\uEF36','\uEF37','\u2130','\u2131','\uEF38','\u210B','\u2110','\uEF39','\uEF3A','\u2112','\u2133','\uEF3B','\uEF3C','\uEF3D','\uEF3E','\u211B','\uEF3F','\uEF40','\uEF41','\uEF42','\uEF43','\uEF44','\uEF45','\uEF46'};
		static readonly char[] _AM_FRK_BASE_PLANE= {'\uEF5D','\uEF5E','\u212D','\uEF5F','\uEF60','\uEF61','\uEF62','\u210C','\u2111','\uEF63','\uEF64','\uEF65','\uEF66','\uEF67','\uEF68','\uEF69','\uEF6A','\u211C','\uEF6B','\uEF6C','\uEF6D','\uEF6E','\uEF6F','\uEF70','\uEF71','\u2128'};
		static readonly char[] _AM_BBB_BASE_PLANE= {'\uEF8C','\uEF8D','\u2102','\uEF8E','\uEF8F','\uEF90','\uEF91','\u210D','\uEF92','\uEF93','\uEF94','\uEF95','\uEF96','\u2115','\uEF97','\u2119','\u211A','\u211D','\uEF98','\uEF99','\uEF9A','\uEF9B','\uEF9C','\uEF9D','\uEF9E','\u2124'};

		#endregion Alphabet in different Fonts

		/// <summary> Script Font Alphabet with higher Plane (Supplement) Characters using Surrogate Characters </summary>
		public static readonly IReadOnlyList<string> AmCal = _AM_CAL;

		/// <summary> Fraktur Script Alphabet with higher Plane (Supplement) Characters using Surrogate Characters </summary>
		public static readonly IReadOnlyList<string> AmFrk = _AM_FRK;

		/// <summary> Hollow Bold Font Alphabet with higher Plane (Supplement) Characters using Surrogate Characters </summary>
		public static readonly IReadOnlyList<string> AmBbb = _AM_BBB;
		public static readonly IReadOnlyList<char> AmCalBasePlane = _AM_CAL_BASE_PLANE;
		public static readonly IReadOnlyList<char> AmFrkBasePlane = _AM_FRK_BASE_PLANE;
		public static readonly IReadOnlyList<char> AmBbbBasePlane = _AM_BBB_BASE_PLANE;

		static readonly AmSymbol AM_QUOTE = new(Typ.Adorn, "\"", Tag.Text, M_BOX, null, Token.Text);
		//static readonly AmSymbol AM_ROOT = new AmSymbol(Typ.Adorn, "√", Tag.Root, STR_ROOT, null, Token.Binary); is ambiguous with AsciiMath, unless you introduce '&' to connect n-ary Operators
		static readonly AmSymbol AM_SQ_ROOT = new(Typ.Adorn, "√", Tag.SqRt, STR_SQRT, null, Token.Unary);

		internal class AmSymbol {
			
			public override string ToString() => Input;

			public AmSymbol(
				Typ symbolTyp,
				string input,
				Tag tag,
				string output,
				string tex,
				Token type,
				string description = null
				//bool isInvisible,
				//bool acc,
				//bool isFunc,
				//string[] rewriteLeftRight,
				//bool notExCopy,
				//string atName,
				//string atVal,
				//string[] codes
			) {
				Description = description;
				SymbolTyp = symbolTyp;
				Input = input;
				Tag = tag;
				Output = output;
				Type = type;
				Tex = tex;
				//IsInvisible = isInvisible;
				//Acc = acc;
				//IsFunc = isFunc;
				//RewriteLeftRight = rewriteLeftRight;
				//NotExCopy = notExCopy;
				//AtName = atName;
				//AtVal = atVal;
				//Codes = codes;
			}

			/// <summary> The Text to enter </summary>
			public readonly string	Input;
			public readonly Tag	Tag;
			public readonly string	Output;
			public readonly Token	Type;
			/// <summary> Alternative TeX Notation </summary>
			public readonly string	Tex;
			public readonly Typ SymbolTyp;

			public string Description;
			public bool	IsInvisible;
			public bool	Acc;
			public bool	IsFunc;
			public string[] RewriteLeftRight;
			public bool	NotExCopy;
			public string	AtName;
			public string	AtVal;
			public string[] Codes;
		}

		/// <summary> All well-known AsciiMath  Symbols, mostly TeX-compliant </summary>
		[SuppressMessage("ReSharper", "StringLiteralTypo")]
		static readonly List<AmSymbol> AM_SYMBOLS = new() {
			#region some greek symbols
			new AmSymbol(Typ.Greek, "alpha", Tag.I, "\u03B1", null, Token.Const)
			, new AmSymbol(Typ.Greek, "beta", Tag.I, "\u03B2", null, Token.Const)
			, new AmSymbol(Typ.Greek, "chi", Tag.I, "\u03C7", null, Token.Const)
			, new AmSymbol(Typ.Greek, "delta", Tag.I, "\u03B4", null, Token.Const)
			, new AmSymbol(Typ.Greek, "Delta", Tag.O, "\u0394", null, Token.Const)
			, new AmSymbol(Typ.Greek, "epsi", Tag.I, "\u03B5", "epsilon", Token.Const)
			, new AmSymbol(Typ.Greek, "varEpsilon", Tag.I, "\u025B", null, Token.Const)
			, new AmSymbol(Typ.Greek, "eta", Tag.I, "\u03B7", null, Token.Const)
			, new AmSymbol(Typ.Greek, "gamma", Tag.I, "\u03B3", null, Token.Const)
			, new AmSymbol(Typ.Greek, "Gamma", Tag.O, "\u0393", null, Token.Const)
			, new AmSymbol(Typ.Greek, "iota", Tag.I, "\u03B9", null, Token.Const)
			, new AmSymbol(Typ.Greek, "kappa", Tag.I, "\u03BA", null, Token.Const)
			, new AmSymbol(Typ.Greek, "lambda", Tag.I, "\u03BB", null, Token.Const)
			, new AmSymbol(Typ.Greek, "Lambda", Tag.O, "\u039B", null, Token.Const)
			, new AmSymbol(Typ.Greek, "lamda", Tag.I, "\u03BB", null, Token.Const)
			, new AmSymbol(Typ.Greek, "Lamda", Tag.O, "\u039B", null, Token.Const)
			, new AmSymbol(Typ.Greek, "mu", Tag.I, "\u03BC", null, Token.Const)
			, new AmSymbol(Typ.Greek, "nu", Tag.I, "\u03BD", null, Token.Const)
			, new AmSymbol(Typ.Greek, "omega", Tag.I, "\u03C9", null, Token.Const)
			, new AmSymbol(Typ.Greek, "Omega", Tag.O, "\u03A9", null, Token.Const)
			, new AmSymbol(Typ.Greek, "phi", Tag.I, FixPhi ? "\u03D5" : "\u03C6", null, Token.Const)
			, new AmSymbol(Typ.Greek, "varPhi", Tag.I, FixPhi ? "\u03C6" : "\u03D5", null, Token.Const)
			, new AmSymbol(Typ.Greek, "Phi", Tag.O, "\u03A6", null, Token.Const)
			, new AmSymbol(Typ.Greek, "pi", Tag.I, "\u03C0", null, Token.Const)
			, new AmSymbol(Typ.Greek, "Pi", Tag.O, "\u03A0", null, Token.Const)
			, new AmSymbol(Typ.Greek, "psi", Tag.I, "\u03C8", null, Token.Const)
			, new AmSymbol(Typ.Greek, "Psi", Tag.I, "\u03A8", null, Token.Const)
			, new AmSymbol(Typ.Greek, "rho", Tag.I, "\u03C1", null, Token.Const)
			, new AmSymbol(Typ.Greek, "sigma", Tag.I, "\u03C3", null, Token.Const)
			, new AmSymbol(Typ.Greek, "Sigma", Tag.O, "\u03A3", null, Token.Const)
			, new AmSymbol(Typ.Greek, "tau", Tag.I, "\u03C4", null, Token.Const)
			, new AmSymbol(Typ.Greek, "theta", Tag.I, "\u03B8", null, Token.Const)
			, new AmSymbol(Typ.Greek, "varTheta", Tag.I, "\u03D1", null, Token.Const)
			, new AmSymbol(Typ.Greek, "Theta", Tag.O, "\u0398", null, Token.Const)
			, new AmSymbol(Typ.Greek, "upsilon", Tag.I, "\u03C5", null, Token.Const)
			, new AmSymbol(Typ.Greek, "xi", Tag.I, "\u03BE", null, Token.Const)
			, new AmSymbol(Typ.Greek, "Xi", Tag.O, "\u039E", null, Token.Const)
			, new AmSymbol(Typ.Greek, "zeta", Tag.I, "\u03B6", null, Token.Const)
			#endregion some greek symbols

			#region binary operation symbols
			//new AmSymbol("+", "mo", "\u0096", null, Token.CONST},
			//new AmSymbol("-", "mo", "\u0096", null, Token.CONST},
			, new AmSymbol(Typ.Operator, "*", Tag.O, "\u22C5", "cdot", Token.Const)
			, new AmSymbol(Typ.Operator, "**", Tag.O, "\u2217", "ast", Token.Const)
			, new AmSymbol(Typ.Operator, "***", Tag.O, "\u22C6", "star", Token.Const)
			, new AmSymbol(Typ.Operator, "//", Tag.O, "/", null, Token.Const)
			, new AmSymbol(Typ.Operator, "\\\\", Tag.O, "\\", "backslash", Token.Const)
			, new AmSymbol(Typ.Operator, "setminus", Tag.O, "\\", null, Token.Const, "Set Subtraction: A\\B = {a: }")
			, new AmSymbol(Typ.Operator, "xx", Tag.O, "\u00D7", "times", Token.Const)
			, new AmSymbol(Typ.Operator, "|><", Tag.O, "\u22C9", "lTimes", Token.Const, "Left inner Semi-Join containing Columns only from the left Relation ")
			, new AmSymbol(Typ.Operator, "><|", Tag.O, "\u22CA", "rTimes", Token.Const, "Right inner Semi-Join containing Columns only from the right Relation ")
			, new AmSymbol(Typ.Operator, "|><|", Tag.O, "\u22C8", "bowTie", Token.Const, "Natural/Inner Join between 2 Relations")
			, new AmSymbol(Typ.Operator, "]><", Tag.O, "⟕", "lJoin", Token.Const, "Left outer Join containing nulls in the right Relation ")
			, new AmSymbol(Typ.Operator, "><[", Tag.O, "⟖", "rJoin", Token.Const, "Right outer Join containing nulls in the left Relation ")
			, new AmSymbol(Typ.Operator, "]><[", Tag.O, "⟗", "oJoin", Token.Const, "Full Outer Join containing nulls in both Relations")
			, new AmSymbol(Typ.Operator, "-:", Tag.O, "\u00F7", "div", Token.Const, "Division ÷ Symbol")
			, new AmSymbol(Typ.Operator, "divide", Tag.O, "-:", null, Token.Definition)
			, new AmSymbol(Typ.Operator, "@", Tag.O, "\u2218", "circ", Token.Const, "Concatenation of Maps")
			, new AmSymbol(Typ.Operator, "o+", Tag.O, "\u2295", "oPlus", Token.Const, "Logical XOR")
			, new AmSymbol(Typ.Operator, "o-", Tag.O, "⊖", "ominus", Token.Const, "Symmetric Set Difference")
			, new AmSymbol(Typ.Operator, "ox", Tag.O, "\u2297", "oTimes", Token.Const, "Logical AND or Tensor Product")
			, new AmSymbol(Typ.Operator, "o.", Tag.O, "\u2299", "oDot", Token.Const, "")
			, new AmSymbol(Typ.Operator, "o/", Tag.O, "⊘", "oSlash", Token.Const, "")
			, new AmSymbol(Typ.Operator, "O.", Tag.O, "\u2A00", "bigodot", Token.Const, "")
			, new AmSymbol(Typ.Operator, "O+", Tag.O, "\u2A01", "bigoplus", Token.Const, "")
			, new AmSymbol(Typ.Operator, "Ox", Tag.O, "\u2A02", "bigotimes", Token.Const, "")
			, new AmSymbol(Typ.Operator, "sum", Tag.O, "\u2211", "∑", Token.UnderOver, "Sum over")
			, new AmSymbol(Typ.Operator, "prod", Tag.O, "\u220F", "∏", Token.UnderOver, "Product over")
			, new AmSymbol(Typ.Operator, "coprod", Tag.O, "\u220F", "∐", Token.UnderOver, "Product over")
			, new AmSymbol(Typ.Operator, "^^", Tag.O, "\u2227", "wedge", Token.Const, "Logical AND")
			, new AmSymbol(Typ.Operator, "^^^", Tag.O, "\u22C0", "bigWedge", Token.UnderOver, "AND resp. Min over")
			, new AmSymbol(Typ.Operator, "vv", Tag.O, "\u2228", "vee", Token.Const, "Logical AND")
			, new AmSymbol(Typ.Operator, "vvv", Tag.O, "\u22C1", "bigVee", Token.UnderOver, "OR resp. Max over")
			, new AmSymbol(Typ.Operator, "nn", Tag.O, "\u2229", "cap", Token.Const, "Set Intersection")
			, new AmSymbol(Typ.Operator, "nnn", Tag.O, "\u22C2", "bigCap", Token.UnderOver, "Set Intersection over ")
			, new AmSymbol(Typ.Operator, "uu", Tag.O, "\u222A", "cup", Token.Const, "Set Union")
			, new AmSymbol(Typ.Operator, "uuu", Tag.O, "\u22C3", "bigCup", Token.UnderOver, "Set Union over")
			, new AmSymbol(Typ.Operator, "bigSqCup", Tag.O, "\u2A06", "", Token.UnderOver, "")
			, new AmSymbol(Typ.Operator, "sqCap", Tag.O, "⊓", "", Token.UnderOver, "")
			, new AmSymbol(Typ.Operator, "sqCup", Tag.O, "⊔", "", Token.UnderOver, "")
			#endregion binary operation symbols
			#region binary relation symbols
			, new AmSymbol(Typ.Relation, "!=", Tag.O, "\u2260", "ne", Token.Const)
			, new AmSymbol(Typ.Relation, ":=", Tag.O, ":=", null, Token.Const)
			//, new AmSymbol(Typ.Relation, "lt", Tag.O, "<", null, Token.Const, "is lighter than ")
			, new AmSymbol(Typ.Relation, "<=", Tag.O, "\u2264", "le", Token.Const, "is lighter or equal to ")
			, new AmSymbol(Typ.Relation, "lt=", Tag.O, "\u2264", "leq", Token.Const, "is lighter or equal to ")
			, new AmSymbol(Typ.Relation, "gt", Tag.O, ">", null, Token.Const, "is greater than ")
			, new AmSymbol(Typ.Relation, ">=", Tag.O, "\u2265", "ge", Token.Const, "is greater or equal to ")
			, new AmSymbol(Typ.Relation, "gt=", Tag.O, "\u2265", "geq", Token.Const, "is greater or equal to ")
			, new AmSymbol(Typ.Relation, "-<", Tag.O, "\u227A", "prec", Token.Const)
			, new AmSymbol(Typ.Relation, "-lt", Tag.O, "\u227A", null, Token.Const)
			, new AmSymbol(Typ.Relation, ">-", Tag.O, "\u227B", "succ", Token.Const)
			, new AmSymbol(Typ.Relation, "-<=", Tag.O, "\u2AAF", "preceq", Token.Const)
			, new AmSymbol(Typ.Relation, ">-=", Tag.O, "\u2AB0", "succeq", Token.Const)
			, new AmSymbol(Typ.Relation, "in", Tag.O, "\u2208", null, Token.Const, " is an Element of ")
			, new AmSymbol(Typ.Relation, "!in", Tag.O, "\u2209", "notIn", Token.Const, " is not an Element of ")
			, new AmSymbol(Typ.Relation, "sub", Tag.O, "\u2282", "subSet", Token.Const, "is proper Sub-Set of ")
			, new AmSymbol(Typ.Relation, "sup", Tag.O, "\u2283", "supSet", Token.Const, "is proper Super-Set of ")
			, new AmSymbol(Typ.Relation, "subE", Tag.O, "\u2286", "subSetEq", Token.Const, "is Sub-Set of ")
			, new AmSymbol(Typ.Relation, "supE", Tag.O, "\u2287", "supSetEq", Token.Const, "is Super-Set of ")
			, new AmSymbol(Typ.Relation, "-=", Tag.O, "\u2261", "equiv", Token.Const)
			, new AmSymbol(Typ.Relation, "~=", Tag.O, "\u2245", "cong", Token.Const)
			, new AmSymbol(Typ.Relation, "~~", Tag.O, "\u2248", "approx", Token.Const)
			, new AmSymbol(Typ.Relation, "prop", Tag.O, "\u221D", "propTo", Token.Const, " is proportional to ")
			#endregion binary relation symbols
			#region binary logical symbols
			, new AmSymbol(Typ.Logic, "and", Tag.O, "\u00A0and\u00A0", null, Token.Const)
			, new AmSymbol(Typ.Logic, "or", Tag.O, "\u00A0or\u00A0", null, Token.Const)
			, new AmSymbol(Typ.Logic, "not", Tag.O, "\u00AC", "neg", Token.Const)
			, new AmSymbol(Typ.Logic, "if", Tag.O, "\u00A0if\u00A0", null, Token.Const)

			, new AmSymbol(Typ.Logic, "=>", Tag.O, "\u21D2", "implies", Token.Const, "Implication: true => true")
			, new AmSymbol(Typ.Logic, "<=>", Tag.O, "\u21D4", "iff", Token.Const, "Logical Equivalence")
			, new AmSymbol(Typ.Logic, "AA", Tag.O, "\u2200", "forAll", Token.Const, "All-Quantor: for all...")
			, new AmSymbol(Typ.Logic, "EE", Tag.O, "\u2203", "exists", Token.Const, "Existential Quantor: Any Exists...")
			, new AmSymbol(Typ.Logic, "_|_", Tag.O, "\u22A5", "bot", Token.Const, "Bottom Element ")
			, new AmSymbol(Typ.Logic, "TT", Tag.O, "\u22A4", "top", Token.Const, "Top Element")
			, new AmSymbol(Typ.Logic, "|--", Tag.O, "\u22A2", "vDash", Token.Const)
			, new AmSymbol(Typ.Logic, "|==", Tag.O, "\u22A8", "models", Token.Const)
			#endregion binary logical symbols
			#region grouping brackets; will create an <mrow> Pair
			, new AmSymbol(Typ.Bracket, "(", Tag.O, "(", "left(", Token.LeftBracket)
			, new AmSymbol(Typ.Bracket, "〖", Tag.O, "〖", "begin", Token.LeftBracket)
			, new AmSymbol(Typ.Bracket, "〗", Tag.O, "〗", "end", Token.RightBracket)
			, new AmSymbol(Typ.Bracket, ")", Tag.O, ")", "right)", Token.RightBracket)
			, new AmSymbol(Typ.Bracket, "[", Tag.O, "[", "left[", Token.LeftBracket)
			, new AmSymbol(Typ.Bracket, "]", Tag.O, "]", "right]", Token.RightBracket)
			, new AmSymbol(Typ.Bracket, "{", Tag.O, "{", null, Token.LeftBracket)
			, new AmSymbol(Typ.Bracket, "}", Tag.O, "}", null, Token.RightBracket)
			, new AmSymbol(Typ.Bracket, "|", Tag.O, "|", null, Token.LeftRightBracket)
			, new AmSymbol(Typ.Bracket, ":|:", Tag.O, "|", null, Token.Const)
			, new AmSymbol(Typ.Bracket, "|:", Tag.O, "|", null, Token.LeftBracket)
			, new AmSymbol(Typ.Bracket, ":|", Tag.O, "|", null, Token.RightBracket)
			//, new AmSymbol(Typ.Bracket, "||", Tag.O, "||", null, Token.LeftRightBracket)
			, new AmSymbol(Typ.Bracket, "(:", Tag.O, "\u2329", "lAngle", Token.LeftBracket)
			, new AmSymbol(Typ.Bracket, ":)", Tag.O, "\u232A", "rAngle", Token.RightBracket)
			, new AmSymbol(Typ.Bracket, "<<", Tag.O, "≪", null, Token.LeftBracket)
			, new AmSymbol(Typ.Bracket, "<<<", Tag.O, "⋘", null, Token.LeftBracket)
			, new AmSymbol(Typ.Bracket, ">>", Tag.O, "≫", null, Token.RightBracket)
			, new AmSymbol(Typ.Bracket, ">>>", Tag.O, "⋙", null, Token.RightBracket)
			, new AmSymbol(Typ.Bracket, "{:", Tag.O, "{:", null, Token.LeftBracket, "Invisible opening Bracket") { IsInvisible = true } //beiden einzigen mit IsInvisible
			, new AmSymbol(Typ.Bracket, ":}", Tag.O, ":}", null, Token.RightBracket, "Invisible closing Bracket") { IsInvisible = true }
			, new AmSymbol(Typ.Bracket, "|__", Tag.O, "\u230A", "lFloor", Token.Const, "Left Side of the Floor Operator; yields the next smaller integer Number")
			, new AmSymbol(Typ.Bracket, "__|", Tag.O, "\u230B", "rFloor", Token.Const, "Right Side of the Floor Operator; yields the next smaller integer Number")
			, new AmSymbol(Typ.Bracket, "|~", Tag.O, "\u2308", "lCeiling", Token.Const, "Left Side of the Ceiling Operator; yields the next larger integer Number")
			, new AmSymbol(Typ.Bracket, "~|", Tag.O, "\u2309", "rCeiling", Token.Const, "Right Side of the Ceiling Operator; yields the next larger integer Number")
			#endregion grouping brackets
			#region miscellaneous symbols
			, new AmSymbol(Typ.Symbol, "int", Tag.O, "\u222B", null, Token.Const, "Integral")
			, new AmSymbol(Typ.Symbol, "iint", Tag.O, "∬", null, Token.Const, "Integral")
			, new AmSymbol(Typ.Symbol, "iiint", Tag.O, "∭", null, Token.Const, "Integral")
			, new AmSymbol(Typ.Symbol, "iiiint", Tag.O, "⨌", null, Token.Const, "Integral")
			, new AmSymbol(Typ.Symbol, "int", Tag.O, "\u222B", null, Token.Const, "Integral")
			, new AmSymbol(Typ.Symbol, "oint", Tag.O, "\u222E", null, Token.Const, "Path-Integral")
			, new AmSymbol(Typ.Symbol, "oiint", Tag.O, "∯", null, Token.Const, "Path-Integral")
			, new AmSymbol(Typ.Symbol, "oiiint", Tag.O, "∰", null, Token.Const, "Path-Integral")
			, new AmSymbol(Typ.Symbol, "aoint", Tag.O, "∳", null, Token.Const, "Path-Integral")
			//No Char in Unicode; {:dx:} looks clumsy, <mrow> cannot be generated and <mo>dx
			, new AmSymbol(Typ.Symbol, "dx", Tag.I, "dx", null, Token.Definition, "Derivation by x")
			, new AmSymbol(Typ.Symbol, "dy", Tag.I, "dy", null, Token.Definition, "Derivation by y")
			, new AmSymbol(Typ.Symbol, "dz", Tag.I, "dz", null, Token.Definition, "Derivation by z")
			, new AmSymbol(Typ.Symbol, "dt", Tag.I, "dt", null, Token.Definition, "Derivation by t (Time)")
			, new AmSymbol(Typ.Symbol, "del", Tag.O, "\u2202", "partial", Token.Const, "Partial Derivation")
			, new AmSymbol(Typ.Symbol, "grad", Tag.O, "\u2207", "nabla", Token.Const, "Gradient resp. Nabla Vector of partial Derivatives")
			, new AmSymbol(Typ.Symbol, "PlusMinus", Tag.O, "\u00B1", "plusminus", Token.Const, "Plus-Minus e.g. for (absolute) Error Estimates")
			, new AmSymbol(Typ.Symbol, "+-", Tag.O, "\u00B1", "pm", Token.Const, "Plus-Minus e.g. for (absolute) Error Estimates")
			, new AmSymbol(Typ.Symbol, "MinusPlus", Tag.O, "∓", "minusplus", Token.Const, "Plus-Minus e.g. for (absolute) Error Estimates")
			, new AmSymbol(Typ.Symbol, "-+", Tag.O, "∓", "mp", Token.Const, "Plus-Minus e.g. for (absolute) Error Estimates")
			, new AmSymbol(Typ.Symbol, "O/", Tag.O, "\u2205", "emptySet", Token.Const, "Empty Set; contains no Elements")
			, new AmSymbol(Typ.Symbol, "oo", Tag.O, "\u221E", "infty", Token.Const, "Infinity Symbol")
			, new AmSymbol(Typ.Symbol, "aleph", Tag.I, "\u2135", null, Token.Const, "Countable Infinity")
			, new AmSymbol(Typ.Symbol, "beth", Tag.I, "ℶ", null, Token.Const, "")
			, new AmSymbol(Typ.Symbol, "daleth", Tag.I, "ℸ", null, Token.Const, "")
			, new AmSymbol(Typ.Symbol, "gimel", Tag.I, "ℷ", null, Token.Const, "")
			, new AmSymbol(Typ.Symbol, ":.", Tag.O, "\u2234", "therefore", Token.Const)
			, new AmSymbol(Typ.Symbol, ":'", Tag.O, "\u2235", "because", Token.Const)
			, new AmSymbol(Typ.Symbol, "'", Tag.O, "\u2032", "prime", Token.Const)
			, new AmSymbol(Typ.Symbol, "tilde", Tag.Over, "~", null, Token.Unary) {Acc = true}
			, new AmSymbol(Typ.Symbol, "\\ ", Tag.O, "\u00A0", null, Token.Const)
			, new AmSymbol(Typ.Symbol, "frown", Tag.O, "\u2322", null, Token.Const)
			, new AmSymbol(Typ.Symbol, "quad", Tag.O, "\u00A0\u00A0", null, Token.Const)
			, new AmSymbol(Typ.Symbol, "qQuad", Tag.O, "\u00A0\u00A0\u00A0\u00A0", null, Token.Const)
			, new AmSymbol(Typ.Symbol, "cDots", Tag.O, "\u22EF", "cdots", Token.Const, "Centered dots")
			, new AmSymbol(Typ.Symbol, "vDots", Tag.O, "\u22EE", "vdots", Token.Const, "Vertical dots")
			, new AmSymbol(Typ.Symbol, "dDots", Tag.O, "\u22F1", "ddots", Token.Const, "Diagonal dots")
			, new AmSymbol(Typ.Symbol, "/_", Tag.O, "\u2220", "angle", Token.Const, "Denotes a (planar) Angle")
			, new AmSymbol(Typ.Symbol, "/_\\", Tag.O, "\u25B3", "triangle", Token.Const, "Triangle Shape")
			, new AmSymbol(Typ.Symbol, "diamond", Tag.I, "\u22C4", null, Token.Const, "Diamond Shape")
			, new AmSymbol(Typ.Symbol, "square", Tag.I, "\u25A1", null, Token.Const, "Square Shape")
			//, new AmSymbol(Typ.Symbol, "spadeSuit", Tag.I, "♠", null, Token.Const, "Spade Shape") leads to parsing Errors!?!
			//, new AmSymbol(Typ.Symbol, "heartSuit", Tag.I, "♡", null, Token.Const, "Heart Shape")
			, new AmSymbol(Typ.Symbol, "diamondSuit", Tag.I, "♢", null, Token.Const, "Diamond Shape")
			, new AmSymbol(Typ.Symbol, "clubSuit", Tag.I, "♣", null, Token.Const, "Club Shape")
			, new AmSymbol(Typ.Symbol, "dotEq", Tag.I, "≐", null, Token.Const, "Diamond Shape")

			, new AmSymbol(Typ.Symbol, "PP", Tag.O, "\u2119", null, Token.Const, "Prime Numbers: 2, 3, 5, 7...")
			, new AmSymbol(Typ.Symbol, "CC", Tag.O, "\u2102", null, Token.Const, "Complex Numbers: i, 1+3i, ...")
			, new AmSymbol(Typ.Symbol, "NN", Tag.O, "\u2115", null, Token.Const, "Natural Numbers: 1,2,3,... ")
			, new AmSymbol(Typ.Symbol, "QQ", Tag.O, "\u211A", null, Token.Const, "Rational Numbers: 1/2, 1/3, 2/3, ...")
			, new AmSymbol(Typ.Symbol, "RR", Tag.O, "\u211D", null, Token.Const, "Real Numbers (top. Closure of Q)")
			, new AmSymbol(Typ.Symbol, "ZZ", Tag.O, "\u2124", null, Token.Const, "Whole/Integer Numbers")
			, new AmSymbol(Typ.Symbol, "Dd", Tag.I, "ⅅ", null, Token.Unary, "Differential") { Acc = true } 
			, new AmSymbol(Typ.Symbol, "dd", Tag.I, "ⅆ", null, Token.Unary, "Differential") { Acc = true } 
			, new AmSymbol(Typ.Symbol, "ee", Tag.I, "ⅇ", null, Token.Unary, "") { Acc = true } 
			, new AmSymbol(Typ.Symbol, "ii", Tag.I, "ⅈ", null, Token.Unary, "") { Acc = true } 
			, new AmSymbol(Typ.Symbol, "jj", Tag.I, "ⅉ", null, Token.Unary, "") { Acc = true } 
			, new AmSymbol(Typ.Symbol, "degree", Tag.O, "°", null, Token.Unary, "Differential") { Acc = true } 
			, new AmSymbol(Typ.Symbol, "ell", Tag.I, "ℓ", null, Token.Unary, "Differential") { Acc = true } 

			, new AmSymbol(Typ.Symbol, "f", Tag.I, "f", null, Token.Unary) { IsFunc = true }
			, new AmSymbol(Typ.Symbol, "g", Tag.I, "g", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Symbol, "h", Tag.I, "h", null, Token.Unary) { IsFunc = true } 
			#endregion miscellaneous symbols
			#region standard functions
			, new AmSymbol(Typ.Function, "lim", Tag.O, "lim", null, Token.UnderOver, "Limit of a Series")
			, new AmSymbol(Typ.Function, "Lim", Tag.O, "Lim", null, Token.UnderOver)
			, new AmSymbol(Typ.Function, "sin", Tag.O, "sin", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "Sin", Tag.O, "Sin", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "cos", Tag.O, "cos", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "Cos", Tag.O, "Cos", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "tan", Tag.O, "tan", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "Tan", Tag.O, "Tan", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "SinH", Tag.O, "SinH", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "sinH", Tag.O, "sinH", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "CosH", Tag.O, "CosH", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "cosH", Tag.O, "cosH", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "TanH", Tag.O, "TanH", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "tanH", Tag.O, "tanH", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "Cot", Tag.O, "Cot", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "cot", Tag.O, "cot", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "sec", Tag.O, "sec", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "Sec", Tag.O, "Sec", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "csc", Tag.O, "csc", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "Csc", Tag.O, "Csc", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "ArcSin", Tag.O, "ArcSin", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "arcSin", Tag.O, "arcSin", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "ArcCos", Tag.O, "ArcCos", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "arcCos", Tag.O, "arcCos", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "arcTan", Tag.O, "arcTan", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "ArcTan", Tag.O, "ArcTan", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "cotH", Tag.O, "cotH", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "CotH", Tag.O, "CotH", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "secH", Tag.O, "secH", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "SecH", Tag.O, "SecH", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "CscH", Tag.O, "CscH", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "cscH", Tag.O, "cscH", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "abs", Tag.O, "abs", null, Token.Unary, "Absolute Value of a real Number") { RewriteLeftRight = new[] {"|", "|"} } 
			, new AmSymbol(Typ.Function, "norm", Tag.O, "norm", null, Token.Unary, "(Matrix-)Norm") { RewriteLeftRight = new[] {"\u2225", "\u2225"} } 
			, new AmSymbol(Typ.Function, "det", Tag.O, "det", null, Token.Unary)  { RewriteLeftRight = new[] {"|", "|"} } //{ IsFunc = true } 
			, new AmSymbol(Typ.Function, "floor", Tag.O, "floor", null, Token.Unary) { RewriteLeftRight = new[] {"\u230A", "\u230B"} } 
			, new AmSymbol(Typ.Function, "ceil", Tag.O, "ceil", null, Token.Unary) { RewriteLeftRight = new[] {"\u2308", "\u2309"} } 
			, new AmSymbol(Typ.Function, "exp", Tag.O, "exp", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "log", Tag.O, "log", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "ln", Tag.O, "ln", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "Ln", Tag.O, "Ln", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "dim", Tag.O, "dim", null, Token.Const)
			, new AmSymbol(Typ.Function, "mod", Tag.O, "mod", null, Token.Const, "Module/Remainder of a Division")
			, new AmSymbol(Typ.Function, "gcd", Tag.O, "gcd", null, Token.Unary, "Greatest common Divisor") { IsFunc = true } 
			, new AmSymbol(Typ.Function, "lcm", Tag.O, "lcm", null, Token.Unary, " Least common Multiple") { IsFunc = true } 
			//, new AmSymbol(Typ.Function, "sup", Tag.O, "lub", null, Token.Const, "Least upper Bound AKA Supremum") conflicts with SuperSet
			//, new AmSymbol(Typ.Function, "inf", Tag.O, "glb", null, Token.Const, "Greatest lower Bound AKA Infimum")
			, new AmSymbol(Typ.Function, "lub", Tag.O, "lub", null, Token.Const, "Least upper Bound AKA Supremum")
			, new AmSymbol(Typ.Function, "glb", Tag.O, "glb", null, Token.Const, "Greatest lower Bound AKA Infimum")
			, new AmSymbol(Typ.Function, "min", Tag.O, "min", null, Token.UnderOver, "Minimum Value")
			, new AmSymbol(Typ.Function, "Min", Tag.O, "Min", null, Token.UnderOver)
			, new AmSymbol(Typ.Function, "max", Tag.O, "max", null, Token.UnderOver, "Maximum Value")
			, new AmSymbol(Typ.Function, "Max", Tag.O, "Max", null, Token.UnderOver)
			, new AmSymbol(Typ.Function, "Log", Tag.O, "Log", null, Token.Unary) { IsFunc = true } 
			, new AmSymbol(Typ.Function, "Abs", Tag.O, "Abs", null, Token.Unary) { NotExCopy = true, RewriteLeftRight = new[] {"|", "|"} } 
			#endregion standard functions
			#region arrows
			, new AmSymbol(Typ.Arrow, "->", Tag.O, "\u2192", "to", Token.Const)
			, new AmSymbol(Typ.Arrow, ">->", Tag.O, "\u21A3", "rightarrowtail", Token.Const)
			, new AmSymbol(Typ.Arrow, "->>", Tag.O, "\u21A0", "twoheadrightarrow", Token.Const)
			, new AmSymbol(Typ.Arrow, ">->>", Tag.O, "\u2916", "twoheadrightarrowtail", Token.Const)
			, new AmSymbol(Typ.Arrow, "|->", Tag.O, "\u21A6", "mapsto", Token.Const)
			, new AmSymbol(Typ.Arrow, "uArr", Tag.O, "\u2191", "upArrow", Token.Const)
			, new AmSymbol(Typ.Arrow, "dArr", Tag.O, "\u2193", "downArrow", Token.Const)
			, new AmSymbol(Typ.Arrow, "rArr", Tag.O, "\u2192", "rightArrow", Token.Const)
			, new AmSymbol(Typ.Arrow, "lArr", Tag.O, "\u2190", "leftArrow", Token.Const)
			, new AmSymbol(Typ.Arrow, "hArr", Tag.O, "\u2194", "leftRightArrow", Token.Const)
			, new AmSymbol(Typ.Arrow, "RArr", Tag.O, "\u21D2", "RightArrow", Token.Const)
			, new AmSymbol(Typ.Arrow, "LArr", Tag.O, "\u21D0", "LeftArrow", Token.Const)
			, new AmSymbol(Typ.Arrow, "HArr", Tag.O, "\u21D4", "LeftRightArrow", Token.Const)
			#endregion arrows
			#region commands with argument
			, AM_SQ_ROOT
			, new AmSymbol(Typ.Adorn, STR_SQRT, Tag.SqRt, STR_SQRT, null, Token.Unary)
			, new AmSymbol(Typ.Adorn, STR_SQRT, Tag.SqRt, STR_SQRT, null, Token.Unary)
			, new AmSymbol(Typ.Adorn, STR_ROOT, Tag.Root, STR_ROOT, null, Token.Binary)
			//, AM_ROOT
			, new AmSymbol(Typ.Adorn, STR_FRAC, Tag.Frac, "/", null, Token.Binary)
			, new AmSymbol(Typ.Adorn, "over", Tag.Frac, "/", null, Token.Infix)
			, new AmSymbol(Typ.Adorn, "/", Tag.Frac, "/", null, Token.Infix)
			, new AmSymbol(Typ.Adorn, "¦", Tag.Under, "¦", "atop", Token.Infix, "for binomial Coefficients use Frac lineThickness=0")
			, new AmSymbol(Typ.Adorn, "stackRel", Tag.Over, STR_STACK_REL, null, Token.Binary, "Same as overSet")
			, new AmSymbol(Typ.Adorn, TEX_OVER_SET, Tag.Over, STR_STACK_REL, null, Token.Binary, "places smaller 1st Arg above 2nd")
			, new AmSymbol(Typ.Adorn, TEX_UNDER_SET, Tag.Under, STR_STACK_REL, null, Token.Binary, "places smaller 1st Arg below 2nd")
			, new AmSymbol(Typ.Adorn, CHR_UNDER, Tag.Sub, CHR_UNDER, null, Token.Infix, "SubScript" + " to Operators or n-dim Variables")
			, new AmSymbol(Typ.Adorn, CHR_OVER, Tag.Sup, CHR_OVER, null, Token.Infix, "SuperScript")
			, new AmSymbol(Typ.Adorn, "hat", Tag.Over, "̂", null, Token.Unary) { Acc = true } 
			, new AmSymbol(Typ.Adorn, "Bar", Tag.Over, "\u033F", null, Token.Unary, "Overline") { Acc = true } 
			, new AmSymbol(Typ.Adorn, "bar", Tag.Over, "\u00AF", "overline", Token.Unary, "Overline") { Acc = true } 
			, new AmSymbol(Typ.Adorn, "ul", Tag.Under, "\u0332", "underline", Token.Unary, "Underline") { Acc = true } 
			, new AmSymbol(Typ.Adorn, STR_VEC, Tag.Over, "⃗", null, Token.Unary, "Vector") { Acc = true } 
			, new AmSymbol(Typ.Adorn, "dot", Tag.Over, "̇", null, Token.Unary, "Time-Derivative") { Acc = true } 
			, new AmSymbol(Typ.Adorn, "ddot", Tag.Over, "̈", null, Token.Unary, "2nd Time-Derivative") { Acc = true } 
			, new AmSymbol(Typ.Adorn, "dddot", Tag.Over, " ⃛", null, Token.Unary, "3nd Time-Derivative") { Acc = true } 
			, new AmSymbol(Typ.Adorn, "ddddot", Tag.Over, "\u20DC", null, Token.Unary, "3nd Time-Derivative") { Acc = true } 
			//, new AmSymbol(Typ.Adorn, "..", M_OVER, "̈ ", null, Token.Unary, "2nd Time-Derivative") { Acc = true } 
			, new AmSymbol(Typ.Adorn, "...", Tag.O, "…", "ldots", Token.Const, "Lower dots") //…
			, new AmSymbol(Typ.Adorn, "overArc", Tag.Over, "\u23DC", "overParen", Token.Unary, "???") { Acc = true } 
			, new AmSymbol(Typ.Adorn, "uBrace", Tag.Under, "\u23DF", "underBrace", Token.UnaryUnderOver, "horiz. Brace below Arg") { Acc = true } 
			, new AmSymbol(Typ.Adorn, "oBrace", Tag.Over, "\u23DE", "overBrace", Token.UnaryUnderOver, "horiz. Brace above Arg") { Acc = true } 
			, new AmSymbol(Typ.Adorn, "text", Tag.Text, "text", null, Token.Text, "Verbatim Text in Formula")
			, new AmSymbol(Typ.Adorn, M_BOX, Tag.Text, M_BOX, null, Token.Text)
			, new AmSymbol(Typ.Adorn, STR_COLOR, Tag.Style, null, null, Token.Binary)
			, new AmSymbol(Typ.Adorn, STR_ID, Tag.Row, null, null, Token.Binary)
			, new AmSymbol(Typ.Adorn, STR_CLASS, Tag.Row, null, null, Token.Binary)
			, new AmSymbol(Typ.Adorn, STR_CANCEL, Tag.Enclose, STR_CANCEL, null, Token.Unary, "Cancelling Terms")
			, AM_QUOTE
			#endregion commands with argument
			#region fonts
			, new AmSymbol(Typ.Font, "bb", Tag.Style, "bb", null, Token.Unary) { AtName = ATTRIB_MATH_VARIANT, AtVal = "bold" } 
			, new AmSymbol(Typ.Font, "mathBf", Tag.Style, "mathBf", null, Token.Unary) { AtName = ATTRIB_MATH_VARIANT, AtVal = "bold" } 
			, new AmSymbol(Typ.Font, "sf", Tag.Style, "sf", null, Token.Unary) { AtName = ATTRIB_MATH_VARIANT, AtVal = "sans-serif" } 
			, new AmSymbol(Typ.Font, "mathSf", Tag.Style, "mathSf", null, Token.Unary) { AtName = ATTRIB_MATH_VARIANT, AtVal = "sans-serif" } 
			, new AmSymbol(Typ.Font, "bbb", Tag.Style, "bbb", null, Token.Unary) { AtName = ATTRIB_MATH_VARIANT, AtVal = "double-struck", Codes = _AM_BBB } 
			, new AmSymbol(Typ.Font, "mathBb", Tag.Style, "mathbb", null, Token.Unary) { AtName = ATTRIB_MATH_VARIANT, AtVal = "double-struck", Codes = _AM_BBB } 
			, new AmSymbol(Typ.Font, "cc", Tag.Style, "cc", null, Token.Unary) { AtName = ATTRIB_MATH_VARIANT, AtVal = "script", Codes = _AM_CAL } 
			, new AmSymbol(Typ.Font, "mathCal", Tag.Style, "mathcal", null, Token.Unary) { AtName = ATTRIB_MATH_VARIANT, AtVal = "script", Codes = _AM_CAL } 
			, new AmSymbol(Typ.Font, "tt", Tag.Style, "tt", null, Token.Unary) { AtName = ATTRIB_MATH_VARIANT, AtVal = "monospace" } 
			, new AmSymbol(Typ.Font, "mathTt", Tag.Style, "mathtt", null, Token.Unary) { AtName = ATTRIB_MATH_VARIANT, AtVal = "monospace" } 
			, new AmSymbol(Typ.Font, "fr", Tag.Style, "fr", null, Token.Unary) { AtName = ATTRIB_MATH_VARIANT, AtVal = "fraktur", Codes = _AM_FRK } 
			, new AmSymbol(Typ.Font, "mathFrak", Tag.Style, "mathfrak", null, Token.Unary) { AtName = ATTRIB_MATH_VARIANT, AtVal = "fraktur", Codes = _AM_FRK } 
			#endregion fonts
		};

		static readonly string[] _STRINGS = {STR_COLOR, STR_CLASS, STR_ID};
		static int CompareNames(AmSymbol s1, AmSymbol s2) => string.Compare(s1.Input, s2.Input, StringComparison.Ordinal);

		static readonly List<string> _SORTED_ASCII_NAMES = new();
		public static readonly IReadOnlyList<string> SORTED_ASCII_NAMES = _SORTED_ASCII_NAMES;
		static readonly Dictionary<string, AmSymbol> _ASCII_NAMES_BY_CHAR = new();
		internal static IReadOnlyDictionary<string, AmSymbol> ASCII_NAMES_BY_CHAR = _ASCII_NAMES_BY_CHAR;

		static Parser() {
			InitSymbols();
		}
		static void InitSymbols() {
			var symLen = AM_SYMBOLS.Count;
			for (var i = 0; i < symLen; i++) {
				var amSymbol = AM_SYMBOLS[i];
				if (amSymbol.Output?.Length > 0 && amSymbol.Type != Token.Infix //.Output[0] != '.'
					) {
					if (_ASCII_NAMES_BY_CHAR.TryGetValue(amSymbol.Output, out var old)) {
						if (old.Input.Length > amSymbol.Input.Length) {
							_ASCII_NAMES_BY_CHAR[amSymbol.Output] = amSymbol;
						}
					} else {
						_ASCII_NAMES_BY_CHAR[amSymbol.Output] = amSymbol;
					}
				}
				var lower = amSymbol.Input[0] + amSymbol.Input.Substring(1).ToLower();
				if (lower != amSymbol.Input) {
					AddAlternative(amSymbol, lower);
				}
				if (string.IsNullOrEmpty(amSymbol.Tex)) {
					continue;
				}
				AddAlternative(amSymbol, amSymbol.Tex);
				lower = amSymbol.Tex[0] + amSymbol.Tex.Substring(1).ToLower();
				if (lower != amSymbol.Tex) {
					AddAlternative(amSymbol, lower);
				}
			}
			RefreshSymbols();
		}

			static void AddAlternative(AmSymbol amSymbol, string lower) => AM_SYMBOLS.Add
				(new AmSymbol(amSymbol.SymbolTyp, lower, amSymbol.Tag, amSymbol.Output, null, amSymbol.Type) {
					Acc = amSymbol.Acc
					, Description = amSymbol.Description
					, AtName = amSymbol.AtName
					, AtVal = amSymbol.AtVal
					, Codes = amSymbol.Codes
					, IsFunc = amSymbol.IsFunc
					, IsInvisible = amSymbol.IsInvisible
					, NotExCopy = amSymbol.NotExCopy
					, RewriteLeftRight = amSymbol.RewriteLeftRight
				});

			static void RefreshSymbols() {
			AM_SYMBOLS.Sort(CompareNames);
			_SORTED_ASCII_NAMES.Clear();
			_SORTED_ASCII_NAMES.Capacity = AM_SYMBOLS.Count;
			_SORTED_ASCII_NAMES.AddRange(AM_SYMBOLS.Select(t => t.Input));
		}

		/// <summary> Defines a new Math-Symbol for the Parser </summary>
		public static void DefineSymbol(string oldStr, string newStr, string tex = "", string description = "") {
			AM_SYMBOLS.Add(new AmSymbol(Typ.Symbol, oldStr, Tag.O, newStr, tex, Token.Definition, description));
			RefreshSymbols(); // this becomes a problem if many symbols are defined!
		}

		/// <summary> remove n characters and any following blanks </summary>
		static string AmRemoveCharsAndBlanks(string str, int n) {
			string st;
			if (n + 1 < str.Length && str[n] == '\\' && str[n + 1] != '\\' && str[n + 1] != ' ') {
				st = str.Substring(n + 1);
			} else {
				st = str.Substring(n);
			}
			var i = 0;
			for (; i < st.Length && st[i] <= 32; i += 1) { }
			return st.Substring(i);
		}

		static int Position(IReadOnlyList<string> arr, string str, int n) {
// return position >=n where str appears or would be inserted
// assumes arr is sorted
			if (n == 0) {
				n = -1;
				var h = arr.Count;
				while (n + 1 < h) {
					var m = (n + h) >> 1;
					if (string.Compare(arr[m], str, StringComparison.Ordinal) < 0) {
						n = m;
					} else {
						h = m;
					}
				}
				return h;
			}
			int i = n;
			for (; i < arr.Count && string.Compare(arr[i], str, StringComparison.Ordinal) < 0; i++) { }
			return i; // i=arr.Length || arr[i]>=str
		}

		#endregion static

		#region Parsing Members

		int _BracketDepth;
		Token _AmPreviousSymbol, _AmCurrentSymbol;

		#endregion Parsing Members

		#region "Styling" Properties

		/// <summary> XML Namespace to apply </summary>
		const string MathMlNs = ""; 
		public const string XML_SCHEMA_MATH_ML = "http://www.w3.org/1998/Math/MathML";

		/// <summary> change it to another color or "" (to inherit) </summary>
		public string MathColor = "blue";

		/// <summary> change to e.g. 1.2fem for larger math </summary>
		public string MathFontSize = "1em";

		/// <summary> change to another family (e.g. "arial") or "" to inherit (works in IE) </summary>
		public string MathFontFamily = "serif";

		// 
		public bool DisplayStyle = true; // puts limits above and below large operators
		public bool ShowAsciiFormulaOnHover = true; // helps students learn ASCIIMath
		public char DecimalSign = '.'; // change to "," if you like, beware of `(1,2)`!

		#endregion "Styling" Properties

		/// <summary> Parses <paramref name="asciiMath"/> into <see cref="Elements.Math"/> </summary>
		public XmlNode ParseAsciiMath(string asciiMath) => AmParseExpr(asciiMath.Replace(@"^\s+", ""), false).Node;

		#region static Parsing Methods

		/// <summary> <see cref="char.IsLetterOrDigit(char)"/> or <see cref="char.IsPunctuation(char)"/> are too broad! </summary>
		public const string StrSeparators = ",.;:";

		public static bool IsSeparator(char chr) => 0 <= StrSeparators.IndexOf(chr);
		public static bool IsSeparator(string str) => str.Length == 1 && IsSeparator(str[0]);
		public static bool IsSeparator(XmlNode child) => child.Name == M_O 
			&& child.FirstChild == child.LastChild 
			&& IsSeparator(child.FirstChild.Value); //.InnerText);

		static char CollectDigits(string str, char chr, ref int pos) {
			while ('0' <= chr && chr <= '9' && pos <= str.Length) {
				chr = ChrAt(str, pos);
				pos++;
			}
			return chr;
		}

		static int MatchPos(string str) {
			var newPos = 0;
			int matchPos = -1;
			var hasMore = true;
			for (int i = 1; i <= str.Length && hasMore; i++) {
				var chr = str.Substring(0, i); //initial substring of length i
				var oldPos = newPos;
				newPos = Position(SORTED_ASCII_NAMES, chr, oldPos);
				if (newPos < SORTED_ASCII_NAMES.Count && str.StartsWith(SORTED_ASCII_NAMES[newPos])) {
					var match = SORTED_ASCII_NAMES[newPos];
					matchPos = newPos;
					i = match.Length;
				}
				hasMore = newPos < SORTED_ASCII_NAMES.Count && string.Compare(str, SORTED_ASCII_NAMES[newPos], StringComparison.Ordinal) >= 0;
			}
			return matchPos;
		}

		static char ChrAt(string str, int newPos) => newPos < str.Length ? str[newPos] : '\0';

		static void AmRemoveBrackets(XmlNode node) {
			string st;
			if (!node.HasChildNodes) {
				return;
			}
			if (node.FirstChild.HasChildNodes && (node.Name == M_FENCED || node.Name == M_MROW)) {
				st = node.FirstChild.FirstChild.Value;
				if (st == "(" || st == "[" || st == "{") {
					node.RemoveChild(node.FirstChild);
				}
			}
			if (node.LastChild.HasChildNodes && (node.Name == M_FENCED || node.Name == M_MROW)) {
				st = node.LastChild.FirstChild.Value;
				if (st == ")" || st == "]" || st == "}") {
					node.RemoveChild(node.LastChild);
				}
			}
		}

		static int MatchingBracePosition(string rest, int fallBack = 0)
		=> rest[0] switch
			{ '{' => rest.IndexOf('}', 1)
			, '(' => rest.IndexOf(')', 1)
			, '[' => rest.IndexOf(']', 1)
			, '|' => rest.IndexOf('|', 1)
			, '〈' => rest.IndexOf('〉', 1)
			, '≪' => rest.IndexOf('≫', 1)
			, '"' => rest.IndexOf('"', 1)
			, _ => fallBack
		};

		static XmlElement CreateNodeFrom(AmSymbol symbol) => CreateMmlNode(symbol.Tag, _Document.CreateTextNode(symbol.Output));

		static string GetCloseBracket(XmlNode node) => node.Attributes?["close"]?.Value ?? ")";
		static void SetOpenBracket(XmlElement xmlElement, string str) => xmlElement.SetAttribute("open", str);
		static void SetCloseBracket(XmlElement xmlElement, string str) => xmlElement.SetAttribute("close", str);
		static string GetOpenBracket(XmlNode node) => node.Attributes?["open"]?.Value ?? "(";

		#endregion

		/// <summary> return maximal initial substring of str that appears in names </summary>
		/// <returns>null if there is none</returns>
		AmSymbol AmGetSymbol(string str) {
			var matchPos = MatchPos(str);
			_AmPreviousSymbol = _AmCurrentSymbol;
			if (matchPos >= 0) {
				_AmCurrentSymbol = AM_SYMBOLS[matchPos].Type;
				return AM_SYMBOLS[matchPos];
			} // if str[0] is a digit or - return maxSubstring of digits.digits
			_AmCurrentSymbol = Token.Const;
			if (str.Length <= 0) {
				return new AmSymbol(Typ.Parsed, "", Tag.O, "", null, Token.Const);
			}
			var newPos = CollectDigits(str, out var isFrac);
			string st;
			Tag tagst;
			if (!isFrac && newPos > 1 || newPos > 2) {
				st = str.Substring(0, newPos - 1);
				tagst = Tag.N;
			} else {
				st = str.Substring(0, 1); //take 1 character
				tagst = ('A' > st[0] || st[0] > 'Z') && ('a' > st[0] || st[0] > 'z') ? Tag.O : Tag.I;
			}
			if (st != "-" || _AmPreviousSymbol != Token.Infix) {
				return new AmSymbol(Typ.Parsed, st, tagst, st, null, Token.Const);
			}
			_AmCurrentSymbol = Token.Infix; //trick "/" into recognizing "-" on second parse
			return new AmSymbol(Typ.Parsed, st, tagst, st, null, Token.Unary) {IsFunc = true};
		}

		int CollectDigits(string str, out bool isFrac) {
			var newPos = 1;
			isFrac = false;
			char chr2 = CollectDigits(str, str[0], ref newPos);
			if (chr2 != DecimalSign) {
				return newPos;
			}
			chr2 = str[newPos];
			if ('0' > chr2 || chr2 > '9') {
				return newPos;
			}
			isFrac = true;
			CollectDigits(str, chr2, ref newPos);
			return newPos;
		}

		[SuppressMessage("ReSharper", "PossibleNullReferenceException")]
		void TrimFenceSeparators(XmlNode newFrag, StringBuilder separators) {
			XmlNode lastSep = CreateSentinel(); newFrag.AppendChild(lastSep);
			var firstSep = CreateSentinel(); newFrag.PrependChild(firstSep);
			//List<XmlElement> curr = new List<XmlElement>();
			for (XmlNode? group = null, prev = lastSep.PreviousSibling; ; ) { //at most every 2nd Node should be a Separator!
				var sep = prev; prev = prev?.PreviousSibling;
				if (!IsSeparator(sep)) {
					if (sep.NextSibling == lastSep) {
						continue; //fine, single Element, just remember to remove lastSep!
					}
					if (group is null) {
						group = CreateMmlNode(Tag.Row); // ReSharper disable once AssignNullToNotNullAttribute
						group.PrependChild(sep.NextSibling);
						newFrag.InsertBefore(group, lastSep);
					}
					group.PrependChild(sep);
					continue; //fine, single Element
				}
				group = null;
				separators.Append(sep.InnerText);
				newFrag.RemoveChild(lastSep);
				if (firstSep == (lastSep = sep)) {
					break;
				}
			}
			newFrag.RemoveChild(firstSep);
			//newFrag.RemoveChild(lastSep); unnecessary, will always be lastSep!
			separators.Length--;
			separators.Revert();
			separators.TrimRepeatingSuffixChar();
		}

		/// <summary> parses str and returns [node,tailStr] </summary>
		ParseResult AmParseS(string rest) {
			var newFrag = _Document.CreateDocumentFragment();
			rest = AmRemoveCharsAndBlanks(rest, 0);
			var symbol = AmGetSymbol(rest); //either a token or a bracket or empty
			if (symbol is null || symbol.Type == Token.RightBracket && _BracketDepth > 0) {
				return new ParseResult(null, rest);
			}
			if (symbol.Type == Token.Definition) {
				rest = symbol.Output + AmRemoveCharsAndBlanks(rest, symbol.Input.Length);
				symbol = AmGetSymbol(rest);
			} // ReSharper disable once SwitchStatementMissingSomeCases
			switch (symbol.Type) {
				case Token.UnderOver:
				case Token.Const: return ParseConst();
				case Token.LeftBracket: return ParseBracketLeft();
				case Token.LeftRightBracket: return ParseBracket();
				case Token.Text: return ParseText();
				case Token.UnaryUnderOver:
				case Token.Unary: return ParseUnary();
				case Token.Binary: return ParseBinary();
				case Token.Infix: {
					rest = AmRemoveCharsAndBlanks(rest, symbol.Input.Length);
					return new ParseResult(CreateMmlNode(Tag.O, _Document.CreateTextNode(symbol.Output)), rest);
				}
				case Token.Space: return ParseSpace();
				default:
					rest = AmRemoveCharsAndBlanks(rest, symbol.Input.Length);
					return new ParseResult(CreateMmlNode(symbol.Tag //its a constant
						, _Document.CreateTextNode(symbol.Output)), rest);
			}

			ParseResult ParseBracketLeft() {
				_BracketDepth++;
				rest = AmRemoveCharsAndBlanks(rest, symbol.Input.Length);
				ParseResult result = AmParseExpr(rest, true);
				_BracketDepth--;
				if (!(result.Node is XmlElement node)) {
					node = CreateMmlNode(Tag.Fenced, result.Node);
					SetCloseBracket(node, "");
				} //This Impl. rather uses explicit m-fenced Elements than the mo Elements with bracket Contents of the JS. 
				if (symbol.Output != "(") {
					SetOpenBracket(node, symbol.IsInvisible ? "" : symbol.Output);
				}
				return NormalizeFenced(node, result.Rest);
			}

			ParseResult ParseBracket() {
				_BracketDepth++;
				rest = AmRemoveCharsAndBlanks(rest, symbol.Input.Length);
				var result = AmParseExpr(rest, false);
				_BracketDepth--;
				XmlElement node;
				var st = GetCloseBracket(result.Node);
				if (st == symbol.Output && rest[0] != ',') { // its an absolute value subterm
					node= result.Node as XmlElement;
					if (node is null) {
						node = CreateMmlNode(Tag.Fenced, result.Node); //
						SetCloseBracket(node, "");
					}
					SetOpenBracket(node, symbol.Output);
					return NormalizeFenced(node, result.Rest);
				} // the "|" is a \mid \u2223 (a vertical bar with a "thick space" on either side) 
				node = CreateMmlNode(Tag.O, _Document.CreateTextNode(STR_V_BAR));
				node = CreateMmlNode(Tag.Row, node);
				return new ParseResult(node, rest);
			}

			ParseResult ParseText() {
				if (symbol != AM_QUOTE) {
					rest = AmRemoveCharsAndBlanks(rest, symbol.Input.Length);
				}
				var i = MatchingBracePosition(rest);
				if (i == -1) {
					i = rest.Length;
				}
				var st = rest.Substring(1, i - 1);
				if (st[0] == ' ') {
					var node = CreateMmlNode(Tag.Space);
					node.SetAttribute(ATTR_WIDTH, ATTR_WIDTH_VALUE);
					newFrag.AppendChild(node);
				}
				newFrag.AppendChild(CreateMmlNode(symbol.Tag, _Document.CreateTextNode(st)));
				if (st.Last() == ' ') {
					var node = CreateMmlNode(Tag.Space);
					node.SetAttribute(ATTR_WIDTH, ATTR_WIDTH_VALUE);
					newFrag.AppendChild(node);
				}
				rest = AmRemoveCharsAndBlanks(rest, i + 1);
				return new ParseResult(CreateMmlNode(Tag.Row, newFrag), rest);
			}

			ParseResult ParseUnary() {
				rest = AmRemoveCharsAndBlanks(rest, symbol.Input.Length);
				var result = AmParseS(rest);
				if (result.Node is null) {
					return new ParseResult(CreateNodeFrom(symbol), rest);
				}
				if (symbol.IsFunc) { // functions hack
					var st = rest.Length > 0 ? rest[0] : 0;
					if (st == '^' || st == '_' || st == '/' || st == '|' || st == ','
						|| st != '(' && symbol.Input.Length == 1 && symbol.Input[0].IsWordChar()) {
						return new ParseResult(CreateNodeFrom(symbol), rest);
					}
					var xmlElement = CreateMmlNode(Tag.Row, CreateNodeFrom(symbol));
					xmlElement.AppendChild(result.Node);
					return new ParseResult(xmlElement, result.Rest);
				}
				AmRemoveBrackets(result.Node);
				if (symbol.Output == STR_SQRT) { // sqrt or root
					if (result.Node.ChildNodes.Count != 3) {
						return new ParseResult(CreateMmlNode(symbol.Tag, result.Node), result.Rest);
					}
					var middle = result.Node.ChildNodes[1];
					if (middle.Name != M_O || middle.InnerText != "&") {
						return new ParseResult(CreateMmlNode(symbol.Tag, result.Node), result.Rest);
					}
					//result.Node.RemoveChild(middle);
					var root = CreateMmlNode(AM_SQ_ROOT.Tag //Am_Root.Tag //
						, result.Node.ChildNodes[0]);
					root.AppendChild(result.Node.ChildNodes[1]);
					return new ParseResult(root, result.Rest);
				}
				if (symbol.RewriteLeftRight is not null) { // abs, floor, ceil
					var xmlElement = CreateMmlNode(Tag.Fenced, result.Node);
					SetOpenBracket(xmlElement, symbol.RewriteLeftRight[0]);
					SetCloseBracket(xmlElement, symbol.RewriteLeftRight[1]);
					return new ParseResult(xmlElement, result.Rest);
				}
				if (symbol.Input == STR_CANCEL) { // cancel
					var xmlElement = CreateMmlNode(symbol.Tag, result.Node);
					xmlElement.SetAttribute(ATTRIB_NOTATION, ATTRIB_NOTATION_UP_DIAGONAL_STRIKE);
					return new ParseResult(xmlElement, result.Rest);
				}
				if (symbol.Acc) { // accent
					var xmlElement = CreateMmlNode(symbol.Tag, result.Node);
					var accNode = CreateMmlNode(Tag.O, _Document.CreateTextNode(symbol.Output));
					if (symbol.Input == STR_VEC && (
						result.Node.Name == M_FENCED 
						&& result.Node.ChildNodes.Count == 1
						&& result.Node.FirstChild.FirstChild.Value?.Length == 1 
						|| result.Node.FirstChild.Value?.Length == 1)) {
						accNode.SetAttribute("stretchy", "false");
					}
					xmlElement.AppendChild(accNode);
					return new ParseResult(xmlElement, result.Rest);
				} // font change command
				if (symbol.Codes is not null) {
					for (var i = 0; i < result.Node.ChildNodes.Count; i++) {
						if (result.Node.ChildNodes[i].Name == M_I ||
							result.Node.Name == M_I) {
							var st = result.Node.Name == M_I
								? result.Node.FirstChild.Value
								: result.Node.ChildNodes[i].FirstChild.Value;
							string newStr = "";
							foreach (var chr in st) {
								if (chr >= 'A' && chr <= 'Z') {
									newStr += symbol.Codes[chr - 'A'];
								} else if (chr >= 'a' && chr <= 'z') {
									newStr += symbol.Codes[chr - 71];
								} else {
									newStr += chr;
								}
							}
							if (result.Node.Name == M_I) {
								result.Node = CreateMmlNode(Tag.O, _Document.CreateTextNode(newStr));
							} else {
								result.Node.ReplaceChild
								(CreateMmlNode(Tag.O, _Document.CreateTextNode(newStr)),
									result.Node.ChildNodes[i]);
							}
						}
					}
				}
				var ret = CreateMmlNode(symbol.Tag, result.Node);
				//if (symbol.AtName is not null)
				ret.SetAttribute(symbol.AtName, symbol.AtVal);
				return new ParseResult(ret, result.Rest);
			}

			ParseResult ParseSpace() {
				rest = AmRemoveCharsAndBlanks(rest, symbol.Input.Length);
				var node1 = CreateMmlNode(Tag.Space);
				node1.SetAttribute(ATTR_WIDTH, ATTR_WIDTH_VALUE);
				newFrag.AppendChild(node1);
				newFrag.AppendChild(CreateNodeFrom(symbol));
				node1 = CreateMmlNode(Tag.Space);
				node1.SetAttribute(ATTR_WIDTH, ATTR_WIDTH_VALUE);
				newFrag.AppendChild(node1);
				return new ParseResult(CreateMmlNode(Tag.Row, newFrag), rest);
			}

			ParseResult ParseBinary() {
				rest = AmRemoveCharsAndBlanks(rest, symbol.Input.Length);
				var result = AmParseS(rest);
				if (result.Node is null) {
					return new ParseResult(CreateMmlNode(Tag.O, _Document.CreateTextNode(symbol.Input)), rest);
				}
				AmRemoveBrackets(result.Node);
				var result2 = AmParseS(result.Rest); //2nd Operand
				if (result2.Node is null) {
					return new ParseResult(CreateMmlNode(Tag.O, _Document.CreateTextNode(symbol.Input)), rest);
				}
				AmRemoveBrackets(result2.Node);
				if (Array.IndexOf(_STRINGS, symbol.Input) >= 0) {
					var i = MatchingBracePosition(rest, rest.Length);
					var st = rest.Substring(1, i - 1);
					var node = CreateMmlNode(symbol.Tag, result2.Node); // ReSharper disable once SwitchStatementMissingSomeCases
					switch (symbol.Input) {
						case STR_COLOR: node.SetAttribute(ATTRIB_MATH_COLOR, st); break;
						case STR_CLASS: node.SetAttribute(STR_CLASS, st); break;
						case STR_ID: node.SetAttribute(STR_ID, st); break;
					}
					return new ParseResult(node, result2.Rest);
				}
				if (symbol.Input == STR_ROOT ||
					symbol.Output == STR_STACK_REL) { //TODO: STR_STACK_REL should place Elements above each other
					newFrag.AppendChild(result2.Node);
				}
				newFrag.AppendChild(result.Node);
				if (symbol.Input == STR_FRAC) {
					newFrag.AppendChild(result2.Node);
				}
				return new ParseResult(CreateMmlNode(symbol.Tag, newFrag), result2.Rest);
			}

			ParseResult ParseConst() {
				var node = CreateNodeFrom(symbol);
				rest = AmRemoveCharsAndBlanks(rest, symbol.Input.Length);
				if (rest.Length <= 0 || rest[0] != '!') {
					return new ParseResult(node, rest);
				}
				node = CreateMmlNode(Tag.Row, node); //<mrow>
				node.AppendChild(CreateMmlNode(Tag.O, _Document.CreateTextNode("!")));
				return new ParseResult(node, rest.Substring(1));
			}

		}

		ParseResult NormalizeFenced(XmlElement node, string rest) {
			if (node.ChildNodes.Count <= 1) {
				return new ParseResult(node, rest);
			}
			var separators = new StringBuilder();
			TrimFenceSeparators(node, separators);
			if (separators.Length > 1 ||
				separators.Length > 0 && separators[0] != ',') {
				node.SetAttribute("separators", separators.ToString());
			}
			return new ParseResult(node, rest);
		}

		ParseResult AmParseI(string rest) {
			rest = AmRemoveCharsAndBlanks(rest, 0);
			var sym1 = AmGetSymbol(rest);
			var result = AmParseS(rest);
			var node = result.Node;
			rest = result.Rest;
			var symbol = AmGetSymbol(rest);
			if (symbol.Type != Token.Infix || symbol.Input == "/") { //Fraction has Precedence
				return new ParseResult(node, rest);
			}
			rest = AmRemoveCharsAndBlanks(rest, symbol.Input.Length);
			//if (symbol.input == "/") result = AmParseIExpr(str); else ...
			result = AmParseS(rest);
			if (result.Node is null) // show box in place of missing argument
			{
				result.Node = CreateMmlNode(Tag.O, _Document.CreateTextNode("\u25A1"));
			} else {
				AmRemoveBrackets(result.Node);
			}
			rest = result.Rest;
			//if (symbol.input == "/") AmRemoveBrackets(node);
			var underOver = sym1.Type == Token.UnderOver || sym1.Type == Token.UnaryUnderOver;
			switch (symbol.Input) {
				case CHR_UNDER:
					var sym2 = AmGetSymbol(rest);
					if (sym2.Input == CHR_OVER) {
						rest = AmRemoveCharsAndBlanks(rest, sym2.Input.Length);
						var res2 = AmParseS(rest);
						AmRemoveBrackets(res2.Node);
						rest = res2.Rest;
						node = CreateMmlNode(underOver ? Tag.UnderOver : Tag.SubSup, node);
						node.AppendChild(result.Node);
						node.AppendChild(res2.Node);
						//node = CreateMmlNode(Tag.Row, node); // We want sum have an Argument to the right! 
					} else {
						node = CreateMmlNode(underOver ? Tag.Under : Tag.Sub, node);
						node.AppendChild(result.Node);
					}
					break;
				case CHR_OVER when underOver:
					node = CreateMmlNode(Tag.Over, node);
					node.AppendChild(result.Node);
					break;
				default:
					node = CreateMmlNode(symbol.Tag, node);
					node.AppendChild(result.Node);
					break;
			}
			if (!sym1.IsFunc) {
				return new ParseResult(node, rest);
			}
			var sym3 = AmGetSymbol(rest);
			if (sym3.Type == Token.Infix || sym3.Type == Token.RightBracket) {
				return new ParseResult(node, rest);
			}
			result = AmParseI(rest);
			node = CreateMmlNode(Tag.Row, node);
			node.AppendChild(result.Node);
			rest = result.Rest;
			return new ParseResult(node, rest);
		}

		ParseResult AmParseExpr(string rest, bool rightBracket) {

			AmSymbol symbol;
			var newFrag = _Document.CreateDocumentFragment(); // Element("dummyMH"); //.CreateDocumentFragment();
			do {
				rest = AmRemoveCharsAndBlanks(rest, 0);
				var result = AmParseI(rest);
				var node = result.Node;
				rest = result.Rest;
				symbol = AmGetSymbol(rest);
				if (symbol.Type == Token.Infix && symbol.Input == "/") { //Fraction has Precedence
					rest = AmRemoveCharsAndBlanks(rest, symbol.Input.Length);
					result = AmParseI(rest);
					if (result.Node is null) // show box in place of missing argument
					{
						result.Node = CreateMmlNode(Tag.O, _Document.CreateTextNode("\u25A1"));
					} else {
						AmRemoveBrackets(result.Node);
					}
					rest = result.Rest;
					AmRemoveBrackets(node);
					node = CreateMmlNode(symbol.Tag, node);
					node.AppendChild(result.Node);
					newFrag.AppendChild(node);
					symbol = AmGetSymbol(rest);
				} else if (node is not null) {
					newFrag.AppendChild(node);
				}
			} while ((symbol.Type != Token.RightBracket &&
				(symbol.Type != Token.LeftRightBracket || rightBracket)
				|| _BracketDepth == 0) && symbol.Output != "");
			if (symbol.Type != Token.RightBracket && 
				symbol.Type != Token.LeftRightBracket) {
				return new ParseResult(newFrag, rest);
			} 
			IsMatrix();
			rest = AmRemoveCharsAndBlanks(rest, symbol.Input.Length);
			if (symbol.IsInvisible) {
				return new ParseResult(newFrag, rest);
			}
			var ret = CreateMmlNode(Tag.Fenced, newFrag);
			if (symbol.Output != ")") {
				SetCloseBracket(ret, symbol.Output);
			}
			return new ParseResult(ret, rest);

			// ReSharper disable once UnusedLocalFunctionReturnValue
			bool IsMatrix() {
				if (newFrag.LastChild?.Name != M_FENCED) {
					return false;
				}
				var right = GetCloseBracket(newFrag.LastChild);
				if (right != ")" && 
					right != "]") {
					return false; //Inner Matrix Nodes must end with ] or )
				}
				string left = GetOpenBracket(newFrag.FirstChild);
				if ((left != "(" || right != ")" || symbol.Output == "}") && 
					(left != "[" || right != "]")) {
					return false;  //Inner Matrix Nodes End Braces must match Start Braces ] or )
				}
				if (!HasMatrixStructure()) {
					return false;
				}
				var colLines = new List<string>();
				var table = CreateMmlNode(Tag.Table, _Document.CreateDocumentFragment());
				var columnLines = colLines;
				var nodesCount = newFrag.ChildNodes.Count;
				for (var i = 0; i < nodesCount; i += 2) { //every 2nd row is only <mo>,</mo>
					AddMatrixRow(); columnLines = null;
				}
				colLines.TrimRepeatingSuffix();
				if (colLines.Count > 1 ||
					colLines.Count > 0 && colLines[0] != COL_LINES_NONE) {
					table.SetAttribute(ATTR_COLUMN_LINES, string.Join(" ", colLines.Skip(1)));
				}
				if (symbol.IsInvisible) {
					table.SetAttribute(ATTRIB_COLUMN_ALIGN, ATTRIB_COLUMN_ALIGN_VALUE);
				}
				newFrag.ReplaceChild(table, newFrag.FirstChild);
				return true;

				void AddMatrixRow() {
					var oldRow = (XmlElement) newFrag.FirstChild;
					var newRow = CreateMmlNode(Tag.Tr); table.AppendChild(newRow);
					for (XmlNode child; (child = oldRow.FirstChild) is not null;) {
						if (child.ChildNodes.Count == 1 &&
							child.FirstChild.FirstChild?.Value == STR_V_BAR) {
							//is columnLine marker - skip it
							columnLines?.Add("solid");
							oldRow.RemoveChild(child); //remove <mrow><mo>|</mo>
						} else {
							columnLines?.Add(COL_LINES_NONE);
						}
						if ((child = oldRow.FirstChild) is null) {
							break;
						}
						oldRow.RemoveChild(child); //remove <mrow><mo>|</mo>
						var cell = newRow.AppendChild(CreateMmlNode(Tag.Td));
						if (child.Name != M_ROW) {
							cell.AppendChild(child);
						} else {
							for (; child.FirstChild is not null;) {
								cell.AppendChild(child.FirstChild);
							}
						}
					}
					columnLines?.Add(COL_LINES_NONE);
					if (newFrag.ChildNodes.Count > 2) {
						newFrag.RemoveChild(newFrag.FirstChild); //remove <mrow>)</mrow>
						newFrag.RemoveChild(newFrag.FirstChild); //remove <mo>,</mo>
					}
				}

				bool HasMatrixStructure() {
					var row = newFrag.FirstChild;
					var numCells = row.ChildNodes.Count;
					for (; ; row = row.NextSibling?.NextSibling) { //every 2nd Node is a ',' Operator
						var open = GetOpenBracket(row);
						var close = GetCloseBracket(row);
						var isMatrix = row?.Name == M_FENCED && // ReSharper disable once PossibleNullReferenceException
							open == left &&
							close == right;
						if (!isMatrix) {
							return false;
						}
						if (row.ChildNodes.Count != numCells) {
							return false;
						}
						if (row == newFrag.LastChild) {
							return true;
						}
						if (!IsSeparator(row.NextSibling)) {
							return false;
						}
					}
				}

			}

		}

		#region HTML Processing

		const string AM_DELIMITER1 = "`";
		static readonly string[] AmDelimiters = {AM_DELIMITER1};

		static string[] SplitByAmDelimiter(string str) => str.Split(AmDelimiters, StringSplitOptions.RemoveEmptyEntries);

		/// <summary>Filters Strings in `Apostrophes` and fixes HTML Artifacts </summary>
		public string AmAutoMathRec(string str) {
//formula is a space (or start of str) followed by a maximal sequence of *two* or more tokens, possibly separated by runs of digits and/or space.
//tokens are single letters (except a, A, I) and ASCIIMathML tokens
			str = _RE0.Replace(str, " `$2`$7");
			var arr = str.Split(AM_DELIMITER1[0]);
			for (var i = 0; i < arr.Length; i++) //single non-english tokens
			{
				if (i % 2 == 0) {
					arr[i] = _RE1.Replace(arr[i], " `$2`$3");
					arr[i] = _RE2.Replace(arr[i], " `$2`$3");
					arr[i] = _RE3.Replace(arr[i], "`$1`");
				}
			}
			str = string.Join(AM_DELIMITER1, arr);
			str = _RE4.Replace(str, "$1`)"); //fix parentheses
			str = _RE5.Replace(str, "$1`$3"); //fix parentheses
			str = _RE6.Replace(str, "` in");
			str = _RE7.Replace(str, "$1`");
			str = _RE8.Replace(str, "$1$2"); //case insensitive!!!
			str = _RE9.Replace(str, "$1");
			return str;
		}

		const string TEX_COMMAND = @"\\[^\W_0-9]+|\\\s|";
		//static readonly Regex RxTexCommand = new Regex(TEX_COMMAND);
		const string AMBIG_AM_TOKEN = @"\b(?:oo|lim|ln|int|oint|del|grad|aleph|prod|prop|sinh|cosh|tanh|cos|sec|pi|tt|fr|sf|sube|supe|sub|sup|det|mod|gcd|lcm|min|max|vec|ddot|ul|chi|eta|nu|mu)(?![a-z])|";
		const string ENGLISH_AM_TOKEN = @"\b(?:sum|ox|log|sin|tan|dim|hat|bar|dot)(?![a-z])|"; //Word begin
		const string SECOND_ENGLISH_AM_TOKEN = @"|\bI\b|\bin\b|\btext\b"; // took if and or not out
		const string SIMPLE_AM_TOKEN = // uuu nnn?
			"NN|ZZ|QQ|RR|CC|TT|AA|EE|sqrt|dx|dy|dz|dt|xx|vv|uu|nn|bb|cc|csc|cot|alpha|beta|delta|Delta|epsilon|gamma|Gamma|kappa|lambda|Lambda|omega|phi|Phi|Pi|psi|Psi|rho|sigma|Sigma|tau|theta|Theta|xi|Xi|zeta";
		const string LETTER = "[a-zA-HJ-Z](?=(?:[^a-zA-Z]|$|" + AMBIG_AM_TOKEN + ENGLISH_AM_TOKEN + SIMPLE_AM_TOKEN + "))|";
		const string TOKEN = LETTER + TEX_COMMAND + @"\d+|[-()[\]{}+=*&^_%\@/<>,\|!:;'~]|\.(?!(?: |$))|"
			+ AMBIG_AM_TOKEN + ENGLISH_AM_TOKEN + SIMPLE_AM_TOKEN;

		static readonly Regex _RE3 = new(@"([{}[\]])");
		static readonly Regex _RE4 = new(@"((^|\s)\([^\W_0-9]{2,}.*?)\)`");
		static readonly Regex _RE5 = new(@"`(\((a\s|in\s))(.*?[^\W_0-9]{2,}\))");
		static readonly Regex _RE6 = new(@"\sin`");
		static readonly Regex _RE7 = new(@"`(\(\w\)[,.]?(\s |\n |$))");
		static readonly Regex _RE8 = new(@"`([0 - 9.]+|e.g | i.e)`(\.?)");
		static readonly Regex _RE9 = new(@"`([0 - 9.]+:)`");

		static readonly Regex _RE0 = new(@"(^|\s)(((" + TOKEN + @")\s?)((" + TOKEN + SECOND_ENGLISH_AM_TOKEN + @")\s?)+)([,.?]?(?=\s|$))");
		static readonly Regex _RE1 = new(@"(^|\s)([b-zB-HJ-Z+*<>]|" + TEX_COMMAND + AMBIG_AM_TOKEN + SIMPLE_AM_TOKEN + @")(\s|\n|$)");
		static readonly Regex _RE2 = new(@"(^|\s)([a-z]|" + TEX_COMMAND + AMBIG_AM_TOKEN + SIMPLE_AM_TOKEN + ")([,.])"); // removed |\d+ for now

		public bool AutoMathRecognize = true;
		public bool NoMathMl = false;

		XmlElement ParseMath(string asciiMathHtml) {
			_BracketDepth = 0;
			//some basic cleanup for dealing with stuff editors like TinyMCE adds
			asciiMathHtml = asciiMathHtml.TrimStart();
			asciiMathHtml = asciiMathHtml.Replace(@"&nbsp;", " ");
			asciiMathHtml = asciiMathHtml.Replace(@"&gt;", ">");
			asciiMathHtml = asciiMathHtml.Replace(@"&lt;", "<");
			var frag = ParseAsciiMath(asciiMathHtml);
			var node = CreateMmlNode(Tag.Style, frag);
			if (MathColor != "") {
				node.SetAttribute(ATTRIB_MATH_COLOR, MathColor);
			}
			if (MathFontSize != "") {
				node.SetAttribute(ATTRIB_FONT_SIZE, MathFontSize);
				node.SetAttribute(ATTRIB_MATH_SIZE, MathFontSize);
			}
			if (MathFontFamily != "") {
				node.SetAttribute(ATTRIB_FONT_FAMILY, MathFontFamily);
				node.SetAttribute(ATTRIB_MATH_VARIANT, MathFontFamily);
			}

			if (DisplayStyle) {
				node.SetAttribute(ATTRIB_DISPLAY_STYLE, "true");
			}
			node = CreateMmlNode(Tag.Math, node);
			if (ShowAsciiFormulaOnHover) {
				node.SetAttribute(ATTRIB_TITLE, asciiMathHtml.TrimStart()); //" " + does not show in Gecko
			}
			return node;
		}

		public void AmProcessNode(XmlNode n, bool lineBreaks, string spanClass) {
			if (spanClass is not null) {
				var frag = _Document.GetElementsByTagName("span");
				for (var i=0;i<frag.Count;i++) {
					if ("AM".Equals(((XmlElement)frag[i]).GetAttribute("class"), StringComparison.OrdinalIgnoreCase)) {
						ProcessNodeR(frag[i],lineBreaks,false);
					}
				}
			} else {
				try {
					string st = n.InnerXml; // look for AM-delimiter on page
					if (new Regex(@"amath|\begin{a?math}").IsMatch(st) 
						|| st ==AM_DELIMITER1) {
						ProcessNodeR(n,lineBreaks,false);
					} else {
						var indexOf = st.IndexOf(AM_DELIMITER1 +" ", StringComparison.Ordinal);
						if (indexOf >= 0 && " <\n".Contains(st
							[indexOf + AM_DELIMITER1.Length])) {
							ProcessNodeR(n, lineBreaks, false);
						}
					}
				} catch { /**/ }
			}
		}

		/// <summary> Recursively processes the Node  </summary>
		int ProcessNodeR(XmlNode n, bool lineBreaks, bool latex) {
			if (n.ChildNodes.Count != 0) {
				if (n.Name == "math") {
					return 0;
				}
				for (var i = 0; i < n.ChildNodes.Count; i++) {
					i += ProcessNodeR(n.ChildNodes[i], lineBreaks, latex);
				}
				return 0;
			}
			if (n.NodeType == XmlNodeType.Comment && !lineBreaks
				|| "form"    .Equals(n.ParentNode?.Name, StringComparison.OrdinalIgnoreCase) 
				|| "textarea".Equals(n.ParentNode?.Name, StringComparison.OrdinalIgnoreCase)) {
				return 0;
			}
			var str = n.Value;
			if (str is null) {
				return 0;
			}
			str = str.Replace(@"\r\n\r\n", "\n\n"); //Keep double Line Breaks
			str = str.Replace(@"\x20 +", " ");
			str = str.Replace(@"\s*\r\n", " ");
			bool match;
			string[] arr;
			if (latex) {
// DELIMITERS:
				match = str.IndexOf('$') >= 0;
				str = str.Replace(@"([^\\])\$", @"$1 \$");
				str = str.Replace(@"^\$", @" \$"); // in case \$ at start of string
				arr = str.Split(new[] {@" \$"}, StringSplitOptions.RemoveEmptyEntries);
				for (var i = 0; i < arr.Length; i++) {
					arr[i] = arr[i].Replace(@"\\\$", @"\$");
				}
			} else {
				match = false;
				//str = str.Replace
				//(new Regex(AmEscape1),
				//	function(){
				//	match = true;
				//	return nameof(AmEscape1(")
				//});
				//str = str.Replace(@"\\ ? end{?a? math
				//	}?/i,
				//	function(){
				//		autoMathRecognize = false;
				//		match = true;
				//		return ""
				//	});
				//	str = str.Replace(@"aMath\b |\\ begin{
				//	a? math
				//}/i,
				//function(){
				//	autoMathRecognize = true;
				//	match = true;
				//	return ""
				//});
				arr = SplitByAmDelimiter(str);
				if (AutoMathRecognize) {
					for (var i = 0; i < arr.Length; i++) {
						if (i % 2 == 0) {
							arr[i] = AmAutoMathRec(arr[i]);
						}
					}
				}
				str = string.Join(AM_DELIMITER1, arr);
				arr = str.Split(AmDelimiters, StringSplitOptions.RemoveEmptyEntries);
				for (var i = 0; i < arr.Length; i++) // this is a problem ************
				{
					arr[i] = arr[i].Replace(@"AmEscape1", AM_DELIMITER1);
				}
			}
			if (arr.Length <= 1 && !match) {
				return 0;
			}
			if (NoMathMl) {
				return 0;
			}
			var frg = StrArr2DocFrag(arr, n.NodeType == XmlNodeType.Comment);
			var len = frg.ChildNodes.Count;
			n.ParentNode?.ReplaceChild(frg, n);
			return len - 1;
		}

		/// <summary> <paramref name="arr"/> enthält abwechselnd klartext und AsciiMath </summary>
		public XmlDocumentFragment StrArr2DocFrag(string[] arr, bool lineBreaks) {
			var newFrag = _Document.CreateDocumentFragment();
			var expr = false;
			foreach (var chr in arr) {
				if (expr) {
					newFrag.AppendChild(ParseMath(chr));
				} else {
					string[] array = lineBreaks ? chr.Split('\n') : new[] {chr};
					newFrag.AppendChild(CreateElementXhtml("span").AppendChild(_Document.CreateTextNode(array[0])));
					for (var j = 1; j < array.Length; j++) {
						newFrag.AppendChild(CreateElementXhtml("p"));
						newFrag.AppendChild(CreateElementXhtml("span").AppendChild(_Document.CreateTextNode(array[j])));
					}
				}
				expr = !expr;
			}
			return newFrag;
		}

		#endregion HTML Processing

	}
		}
}