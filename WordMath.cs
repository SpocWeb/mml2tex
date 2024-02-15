using System.Text.RegularExpressions;
using System.Xml;

namespace org.SpocWeb.root.Data.xmls.MathML;

/// <summary> Static Methods to <see cref="Parse"/> and convert <see cref="ToAsciiMath"/> </summary>
public static class WordMath
{
	/// <summary> Parses <paramref name="wordMath"/> into <see cref="Elements.Math"/> </summary>
	/// <remarks>
	/// WordMath linear Notation uses '\' for Escaping and to start Commands
	/// </remarks>
	public static XmlNode Parse(string wordMath, StringComparison comparison = StringComparison.Ordinal)
		=> AsciiMath.Parse(ToAsciiMath(wordMath, comparison));

	/// <summary>Converts Word's linear Math Notation into AsciiMath </summary>
	/// <param name="wordMath">the Math Notation obtained from Word Formulas switched to 'linear' </param>
	/// <param name="comparison">Flag to perform the search for Math Functions case-sensitive (i.e. lower-case)</param>
	/// <param name="singleCharIdentifiers">when false, puts Quotes around all consecutive Word Characters to mark them as Identifiers, printed in Arial.
	/// When true, each character is identified as an individual Variable and printed in Math Style (italic, serif) </param>
	/// <param name="preserveSpaces">Flag to escape Spaces in the Formula which are stripped when false </param>
	[Pure] public static string ToAsciiMath(string wordMath
		, StringComparison comparison = StringComparison.Ordinal
		, bool singleCharIdentifiers = false
		, bool preserveSpaces = true) {
		var s = singleCharIdentifiers ? wordMath : RX_SUB_SUP.Replace(wordMath, RX_SUB_SUP_REPLACE);
		var sb = new StringBuilder(s);
		sb.Replace("_\"\"", ""); //remove empty Quotes
		sb.Replace("^\"\"", "");
		sb.Replace("∑", "sum"); //otherwise Index instead of sub/super
		if (preserveSpaces) {
			sb.Replace(" ", @"\ "); //keep Spaces in the Formula by escaping them
		}
		sb.RemoveAll(INVISIBLE);
		sb.ReplaceAll(SEP_ROW, out _, "),(");
		sb.ReplaceAll(SEP_COL, out _, ",");
		sb.ReplaceAll(BRACE_OPEN, out _, "{:");
		sb.ReplaceAll(BRACE_CLOSE, out _, ":}");
		sb.Translate(INDEX_LO, "_"); // \ below
		sb.Translate(INDEX_HI, "^"); // \ above
		//Replace all accidentally quoted Keywords with their lower-case: 
		//foreach (var name in SORTED_ASCII_NAMES) {
		//	str = str.Replace('"' + name + '"', name);
		//}
		var str = sb.ToString();
		var matches = XPosition.RX_QUOTED_WORD.Matches(str);
		for (var i = matches.Count; --i >= 0;) {
			//Replace all accidentally quoted Keywords with their proper-case: 
			Match match = matches[i];
			var value = match.Groups["w"].Value;
			var position = AsciiMath.Parser.SORTED_ASCII_NAMES.PositionOf(value, comparison);
			if (position < 0) {
				continue;
			}
			str = str.Remove(match.Index, match.Length);
			str = str.Insert(match.Index, AsciiMath.Parser.SORTED_ASCII_NAMES[position]);
		}
		return str;
	}

	#region constants

	static readonly Regex RX_SUB_SUP = new(@"""?(?'name'[^\W_0-9]+)""?(_""?(?'lo'[^\W_0-9]+)""?)?(\^""?(?'hi'[^\W_0-9]+)""?)?(_""?(?'lo'[^\W_0-9]+)""?)?");

	/// <summary> hard-code the order : first _lower then ^higher Index </summary>
	const string RX_SUB_SUP_REPLACE = "\"${name}\"_\"${lo}\"^\"${hi}\"";

	/// <summary> {■, (■ or |■ indicate the Start of a Matrix or Determinant </summary>
	/// <remarks>to be detected as a Matrix in AsciiMath, all Rows must have the same #Cells </remarks>
	public const string MATRIX = "■";

	/// <summary> Must be converted to '),(' preferably matching the outer Columns </summary>
	public const string SEP_ROW = "@";

	/// <summary> Must be converted to ',' </summary>
	public const string SEP_COL = "&";

	/// <summary> Substituted with invisible {: </summary>
	public const string BRACE_OPEN = "├〖";

	/// <summary> Substituted with invisible :} </summary>
	public const string BRACE_CLOSE = "┤〗";

	/// <summary> Substituted with "^" </summary>
	public const string INDEX_HI = "┴";

	/// <summary> Substituted with "_" </summary>
	public const string INDEX_LO = "┬";

	/// <summary> Substituted with empty "" </summary>
	public static readonly string INVISIBLE = Regex.Unescape("■▒\u2061\u2062\u2063\u2064\u2066\u2067\u2068\u2069");

	#endregion constants

}

/// <summary> Extension Method Copies </summary>
static class XStringBuilder {

	/// <summary>Indicates whether <paramref name="chr"/> is '_' or a letter or a decimal digit.</summary>
	[Pure] internal static bool IsWordChar(this char chr) => chr == '_' || char.IsLetterOrDigit(chr); 

	/// <summary> Returns the Position of <paramref name="search"/> in <paramref name="strings"/> </summary>
	/// <returns> <see cref="int.MinValue"/> when <paramref name="search"/> was not found</returns>
	[Pure] internal static int PositionOf(this IEnumerable<string> strings
		, string search, StringComparison comparison) 
		=> strings.GetEnumerator().PositionOf(search, comparison);

	/// <summary> Returns the Position of <paramref name="search"/> in <paramref name="strings"/> </summary>
	/// <returns> <see cref="int.MinValue"/> when <paramref name="search"/> was not found</returns>
	[Pure] internal static int PositionOf(this IEnumerator<string> strings
		, string search, StringComparison comparison) {
		var i = -1;
		for(; strings.MoveNext();) { ++i;
			if (search.Equals(strings.Current, comparison)) {
				return i; // No warning, the targets will just be ignored
			}
		}
		return int.MinValue;
	}

	/// <summary>translates a string by replacing all <paramref name="forbiddenChars"/> with the respective <paramref name="replaceChars"/> Characters</summary>
	/// <returns>the number of Replacements done</returns>
	/// <remarks>
	/// The Semantics of removing <paramref name="forbiddenChars"/> Characters exceeding <paramref name="replaceChars"/> Length is unnecessary.
	/// It is easy to do a second Call to RemoveAll.
	/// optional (<see langword="null"/> or empty => all Characters are removed) String of Replace Characters;
	/// when fewer than <paramref name="forbiddenChars"/>, the the last Replace Character is repeated.
	/// </remarks>
	public static StringBuilder Translate(this StringBuilder str, string forbiddenChars, string replaceChars
		, int? stopp = null, int start = 0) {
		//inefficient due to Memory Locality: 
		//for (int i = stopp; --i >= 0;) { //
		//	str.Replace(forbidden[i], replace[i < replace.Length ? i : replace.Length - 1]);
		//} it is probably more efficient to pass (streaming) once through the long str testing ALL forbidden than otherwise
		for (int i = stopp ?? str.Length; --i >= start; ) {
			var index = forbiddenChars.IndexOf(str[i]);
			if (index < 0) {
				continue;
			}
			str[i] = replaceChars
			[index < replaceChars.Length 
				?index : replaceChars.Length - 1];
		}
		return str;
	}

	/// <summary>Removes all <paramref name="charsToRemove"/> Characters and the last</summary>
	/// <remarks>the last can be removed separately</remarks>
	public static int RemoveAll(this StringBuilder str, string charsToRemove, int? stop = null, int start = 0)
		=> str.ReplaceAll(charsToRemove, out _, "", stop, start);

	/// <summary>Replaces all <paramref name="charsToReplace"/> Characters by replace and the last</summary>
	/// <remarks>the last can be removed separately</remarks>
	public static
		int ReplaceAll(this StringBuilder str, string charsToReplace, out int first
			, string replace = "", int? stop = null, int start = 0) {
		int last = first = -1;
		for (int i = stop ?? str.Length; --i >= start; ) {
			if (charsToReplace.IndexOf(str[i]) < 0) {
				continue;
			}
			str.Remove(i, 1);
			str.Insert(i, replace);
		}
		return last;
	}

	public static void TrimRepeatingSuffixChar(this StringBuilder sb) {
		if (sb.Length <= 0) {
			return;
		}
		int j = sb.Length - 1;
		var last = sb[j];
		for (; --j >= 0;) {
			if (last != sb[j]) {
				break;
			}
			sb.Length--;
		}
	}

	public static void TrimRepeatingSuffix<T>(this IList<T> sb) {
		if (sb.Count <= 0) {
			return;
		}
		int j = sb.Count - 1;
		var last = sb[j];
		for (; --j >= 0;) {
			if (!last.Equals(sb[j])) {
				break;
			}
			sb.RemoveAt(j);
		}
	}

	public static void Revert(this StringBuilder sb) {
		for (int i = sb.Length >> 1; --i >= 0; ) {
			(sb[i], sb[sb.Length - i -1]) = (sb[sb.Length - i -1], sb[i]);
		}
	}

}