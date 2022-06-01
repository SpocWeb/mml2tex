using System.Diagnostics.Contracts;
using System.Xml;
using System.Xml.Xsl;
using org.SpocWeb.root.Extensions.enumerables;

namespace org.SpocWeb.root.Data.xmls.MathML {

	/// <summary> Methods to clean MathML and convert it to and from OfficeMathMl </summary>
	public static class OoMathMl {

		public const string MATH_ML_ROOT = "<math xmlns='" + AsciiMath.Parser.XML_SCHEMA_MATH_ML + "'>";

		/// <summary> The XML resulting from the <paramref name="mathMl"/> can be embedded into a Word Formula </summary>
		public static XmlElement MathMlToOfficeMathMl(this XmlNode mathMl, int removeRowLevels = 1) {
			mathMl.RemoveRedundantParentheses("", removeRowLevels);
			var mathMlText = MATH_ML_ROOT + mathMl.OuterXml + "</math>";
			var nodeReader = XmlReader.Create(new StringReader(mathMlText));
			var nodeWriter = new XmlNodeWriter();
			Mml2OMml.Transform(nodeReader, nodeWriter);
			return nodeWriter.Document.DocumentElement;
		}

		public static void RemoveRedundantParentheses(this XmlNode mathMl, string xmlNs, int removeRowLevels = 1) {
			if (0 < removeRowLevels--) {
				mathMl.RemoveRedundantParentheses(AsciiMath.Parser.M_FENCED, xmlNs);
			}
			for (; 0 < removeRowLevels-- //when too many mRow Elements are removed, grouping Characters like Sum are sized minimally (in PDF only)
				&& 0 < mathMl.RemoveRedundantParentheses(AsciiMath.Parser.M_ROW, xmlNs);) { } //in Word and MathML everything still looks fine
		}

		/// <summary>Recursively removes Nodes of <paramref name="nodeType"/> from <paramref name="mathMl"/> when they are alone in Indexes, Fractions or other Locations </summary>
		/// <returns>the Number of Elements removed</returns>
		public static int RemoveRedundantParentheses(this XmlNode mathMl, string nodeType, string xmlNs) {
			var ret = 0;
			var brackets = mathMl.SelectNodes("//" + xmlNs + nodeType, mathMl.GetAllNamespaces());
			foreach (XmlElement bracket in brackets) {
				if (bracket.HasAttributes ||
					bracket.ChildNodes.Count != 1) {
					ret += ReplaceInvisibleFenceWithRow(bracket);
					continue; //reicht nicht aus, kann eine mrow mit mehreren Elementen enthalten
				}
				var singleChild = bracket.FirstChild;
				if (nodeType == AsciiMath.Parser.M_ROW && 
					singleChild.LocalName == AsciiMath.Parser.M_ROW) {
					bracket.RemoveChild(singleChild);
					for (; singleChild.FirstChild is not null;) {
						bracket.AppendChild(singleChild.FirstChild);
					}
					++ret;
					continue;
				}
				var parent = bracket.ParentNode;
				int pos    = parent.positionOf(bracket);
				if (nodeType != AsciiMath.Parser.M_ROW) {
					switch (parent.LocalName) {       //These Elements have distinct Positions and don't need extra Brackets; 
						case AsciiMath.Parser.M_SUB_SUP: //3 Elemente, aber Bezug für mittleres nicht klar
						case AsciiMath.Parser.M_SUB:     //2 Elemente 
						case AsciiMath.Parser.M_SUP: if (pos == 0) {
								continue;
							}
							break;
						case AsciiMath.Parser.M_MATH:       //also remove outer Brackets
						case AsciiMath.Parser.M_UNDER_OVER: //3 Elemente
						case AsciiMath.Parser.M_UNDER:      //2 Elemente
						case AsciiMath.Parser.M_OVER:
						case AsciiMath.Parser.M_FRAC: break; //2 Elemente
						//case AsciiMath.Parser.M_ROW: if (nodeType == AsciiMath.Parser.M_ROW) break; continue; don't remove mRow. otherwise the separators=',' must be set to prevent commas
						default: continue;
					}
				}
				var child = bracket.RemoveChild(singleChild);
				parent.ReplaceChild(child, bracket);
				++ret;
			}
			return ret;
		}

		static int ReplaceInvisibleFenceWithRow(XmlElement bracket) {
			if (bracket.LocalName != AsciiMath.Parser.M_FENCED 
				|| bracket.Attributes.Count != 2 
				|| "" != bracket.GetAttribute("open")
				|| "" != bracket.GetAttribute("close")) {
				return 0;
			}
			var mRow = bracket.OwnerDocument.CreateElement(bracket.Prefix, AsciiMath.Parser.M_ROW, bracket.NamespaceURI);
			bracket.ParentNode.ReplaceChild(mRow, bracket);
			for (; bracket.FirstChild is not null;) {
				mRow.AppendChild(bracket.FirstChild);
			}
			return 1;
		}

		/// <summary> The MathML resulting from the <paramref name="oMathMl"/> can be used e.g. on Web-Pages </summary>
		public static XmlElement OfficeMathMlToMathMl(this XmlNode oMathMl, int removeRowLevels = 1) {
			var oMathMlText = MATH_ML_ROOT + oMathMl.OuterXml + "</math>";
			var nodeReader = XmlReader.Create(new StringReader(oMathMlText));
			var nodeWriter = new XmlNodeWriter();
			OMml2Mml.Transform(nodeReader, nodeWriter);
			var mathMl = nodeWriter.Document.DocumentElement;
			mathMl.RemoveRedundantParentheses("mml:", removeRowLevels);
			return mathMl;
		}

		static        XslCompiledTransform _Mml2OMmlXsl;
		static        XslCompiledTransform _OMml2MmlXsl;
		public static XslCompiledTransform Mml2OMml => _Mml2OMmlXsl ??= ReadXslt("MML2OMML.XSL");

		public static XslCompiledTransform OMml2Mml
			=> _OMml2MmlXsl ??= ReadXslt("OMml2Mml.XSL".ToUpperInvariant());

		static XslCompiledTransform ReadXslt(string fileName) {
			var oMml2MmlXsl = new XslCompiledTransform();
			var xmlReader = XmlReader.Create(new StreamReader(new MemoryStream
				(typeof(OoMathMl).GetEmbeddedResource("OfficeMathML." + fileName))));
			oMml2MmlXsl.Load(xmlReader);
			return oMml2MmlXsl;
		}

		[Pure] public static byte[] GetEmbeddedResource(this Type type, string relPathWithDots) {
			var    docStream = type.Assembly.GetManifestResourceStream(type.Namespace + "." + relPathWithDots);
			byte[] bytes;
			using var memoryStream = new MemoryStream();
			docStream.CopyTo(memoryStream);
			bytes = memoryStream.ToArray();
			return bytes;
		}
	}
}