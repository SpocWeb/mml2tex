using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

namespace org.SpocWeb.root.Data.xmls.MathML {

	public class UnknownMathMlNodeException : Exception { }

	public class UnknownMathMlElementException : Exception { }

	public class UnknownMathMlAttributeTypeException : Exception { }

	/// <summary> Generic Xml-Serializiation using static Methods, Reflection/Attributes and a String Writer </summary>
	/// <remarks>
	/// Test at: <a href='https://www.tutorialspoint.com/mathml'></a> 
	/// </remarks>
	public static class MathMlSerializer {
		public static bool UseQuote;

		public static string WriteTo(this AMathMlNode node) {
			var writer = new StringWriter();
			//var xmlWriter = XmlWriter.Create(writer
			//	, new XmlWriterSettings{ OmitXmlDeclaration = true, Indent = false });
			//var ser = new XmlSerializer(node.GetType());
			//ser.Serialize(xmlWriter, node);
			WriteTo(node, writer);
			return writer.ToString();
		}

		public static void WriteTo(this AMathMlNode node, TextWriter writer) {
			switch (node) {
				case MElement mathMlElement:	WriteTo(mathMlElement, writer); return;
				case MathMlTextNode textNode:	writer.Write(textNode.Content); return;
			}
			throw new UnknownMathMlNodeException();
		}

		public static void WriteTo(this AMathMlNode node, XmlWriter writer) {
			switch (node) {
				case MElement mathMlElement:	WriteTo(mathMlElement, writer); return;
				case MathMlTextNode textNode:	writer.WriteString(textNode.Content); return;
			}
			throw new UnknownMathMlNodeException();
		}

		internal static void WriteTo(this MElement element, XmlWriter writer) {
			Type type = element.GetType();
			var elementName = GetElementName(type);
			writer.WriteStartElement(elementName);
			var attributes = GetAttributes(type, element);
			foreach (var attribute in attributes) {
				writer.WriteAttributeString(attribute.Item1, attribute.Item3);
			}
			foreach (var node in element.Children) {
				WriteTo(node, writer);
			}
			writer.WriteEndElement();
		}

		internal static void WriteTo(this MElement element, TextWriter writer) {
			Type type = element.GetType();
			var elementName = GetElementName(type);
			writer.Write('<');
			writer.Write(elementName);
			var attributes = GetAttributes(type, element);
			foreach (var attribute in attributes) {
				writer.Write(' ');
				writer.Write(attribute.Item1);
				writer.Write('=');
				writer.Write(UseQuote ? '\'' : '"');
				writer.Write(attribute.Item3); //TODO: escape this using an XmlFormatter 
				writer.Write(UseQuote ? '\'' : '"');
			}
			writer.Write('>');
			foreach (var node in element.Children) {
				WriteTo(node, writer);
			}
			writer.Write('<');
			writer.Write('/');
			writer.Write(elementName);
			writer.Write('>');
		}

		// ReSharper disable once SuggestBaseTypeForParameter
		static string GetElementName(this Type type) {
			var typeAttribute = type.GetCustomAttribute<XmlTypeAttribute>();
			if (typeAttribute is not null) {
				return typeAttribute.TypeName;
			}
			//var elementName = type.GetCustomAttribute<XmlElementAttribute>();
			//return elementName is not null ? elementName.ElementName : type.Name;
			var rootAttribute = type.GetCustomAttribute<XmlRootAttribute>();
			return rootAttribute?.ElementName ?? type.Name.ToLower();
		}

		public static IEnumerable<Tuple<string, int, string>> GetAttributes(this Type type, object element) {
			var attributes = new List<Tuple<string, int, string>>();
			var properties = type.GetProperties().Where(p => p.SetMethod is not null);
			foreach (var property in properties) {
				property.ReadPropertyValue(element, attributes);
			}
			return attributes.OrderBy(a => a.Item2);
		}

		static void ReadPropertyValue(this PropertyInfo property, object element
			, ICollection<Tuple<string, int, string>> attributes) {
			var attributeName = property.GetAttributeName(out var defaultPropertyValue, out int attributeOrderIndex);
			if (attributeName.Length <= 0) {
				return;
			}
			var propertyValue = property.GetValue(element, null);
			if (propertyValue is null) {
				return;
			}
			var writer = new XmlStringWriter();
			writer.SerializeAttributeValue(propertyValue);
			var attributeValue = writer.ToString().Trim();
			writer.Length = 0;
			writer.SerializeAttributeValue(defaultPropertyValue);
			var defaultAttributeValue = writer.ToString();

			if (attributeValue != defaultAttributeValue) {
				attributes.Add(new Tuple<string, int, string>(attributeName, attributeOrderIndex, attributeValue));
			}
		}

		public static string GetAttributeName(this PropertyInfo property
			, out object defaultPropertyValue, out int attributeOrderIndex) {
			attributeOrderIndex = 100;
			defaultPropertyValue = null;
			var attributeName = property.Name.ToLower().Replace('_', '-');//ToSeparated(@"-", true).ToString();
			var propertyAttributes = property.GetCustomAttributes(true);
			foreach(var propertyAttribute in propertyAttributes) {
				switch (propertyAttribute) { 
					case XmlIgnoreAttribute _: return ""; 
					//case IgnoreDataMemberAttribute ignore: return;
					case XmlElementAttribute name:		attributeName = name.ElementName; break;
					case XmlAttributeAttribute name:	attributeName = name.AttributeName; break;
					case XmlAttribute name:				attributeName = name.Name; break;
					//case OrderAttribute mlAttributeOrderIndex:
					//	attributeOrderIndex = mlAttributeOrderIndex.OrderIndex;
					//	break;
					case DefaultValueAttribute fallBack:
						defaultPropertyValue = fallBack.Value;
						break;
				}
			}
			//Rather use _ for - than to generally Transform CamelCase into -! 
			return attributeName;
		}

		static void SerializeAttributeValue(this TextWriter writer, object value) {
			switch (value) {
				case null: return;
				case double d: writer.Write(d);return;
				case bool b: writer.Write(b);return;
				case char c: writer.Write(c);return;
				case string s: writer.Write(s);return;
				case ITextWriteAble writeAble: writeAble.WriteTo(writer, long.MaxValue); return;
				case Enum enm: writer.WriteEnum(enm); return;
				case IList list:
					var i = list.Count - 1;
					var lastItem = list[i];
					for (; --i >= 0; ) { //compress List by removing repeating Items
						if (!Equals(lastItem, list[i])) {
							break;
						}
						//list.RemoveAt(i+1);
					}
					for (var k = 0; ; k++) {
						var item = list[k];
						writer.SerializeAttributeValue(item);
						if (k > i) {
							break;
						}
						writer.Write(' ');
					}
					return;
				default: writer.Write(value); return;
			}
		}

		public static void WriteEnum(this TextWriter writer, Enum value) => writer.Write(value.ToEnumString(true));

		public static string ToEnumString(this Enum value, bool? toLower = null) {
			var enumerationType = value.GetType();
			var name = value.ToString();
			var member = enumerationType.GetMember(name); //Enum.GetName(enumerationType, value));
			var attribute = member[0].GetCustomAttribute<XmlEnumAttribute>(true);
			if (attribute is not null) {
				return attribute.Name;
			}
			return toLower switch {
				true => name.ToLower()
				, null => name
				, _ => name.ToUpper()
			};
		}

	}
}