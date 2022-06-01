using System.Collections;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

namespace org.SpocWeb.root.Data.xmls.MathML {

	public static class XXmlReader {

		/// <summary> reads all Properties of <paramref name="obj"/> by parsing the Xml-Attributes of <paramref name="reader"/>  </summary>
		/// <remarks>
		/// Considers all Xml-Attributes for Names and Default Values.
		/// </remarks>
		public static void ReadAttributes(this XmlReader reader, object obj, IFormatProvider? cultureInfo = null) {
			var typ = obj.GetType(); //better to iterate over all Props, since we would need to 
			foreach (var property in typ.GetProperties(BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public)) {
				var attributeName = property.GetAttributeName(out object _, out int _);
				if (attributeName.Length <= 0 
					|| !reader.MoveToAttribute(attributeName)) {
					continue;
				}
				var value = reader.ReadAttribute(property.PropertyType, cultureInfo);
				property.SetValue(obj, value);
			}
			/*while (reader.MoveToNextAttribute()) { this requires searching matching Props by their Attributes which is inefficient!
				var property = typ.GetProperty(reader.Name, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
				if (property is null) {
					continue;
				}
				var value = reader.ReadAttribute(property.PropertyType);
				property.SetValue(obj, value);
			}*/
			reader.MoveToContent();
		}

		/// <summary> Parses the current <paramref name="reader"/>.<see cref="XmlReader.Value"/> as <paramref name="type"/> </summary>
		public static object ReadAttribute(this XmlReader reader, Type type, IFormatProvider? cultureInfo = null) {
			if (!typeof(IXmlSerializable).IsAssignableFrom(type)) {
				return type.Parse(reader.Value, cultureInfo);
			}
			var value = Activator.CreateInstance(type);
			((IXmlSerializable) value).ReadXml(reader);
			return value;
		}

		/// <summary> Parses <paramref name="strValue"/> into <paramref name="type"/></summary>
		/// <remarks>
		/// Can also parse <see cref="IList"/> Derivates with empty Constructors.
		/// </remarks>
		public static object Parse(this Type type, string strValue, IFormatProvider? cultureInfo = null) {
			if (!typeof(IList).IsAssignableFrom(type)) {
				return typeof(Enum).IsAssignableFrom(type)
					? Enum.Parse(type, strValue.Replace('-', '_'), true)
					: Convert.ChangeType(strValue, type, cultureInfo);
			}
			if (!type.IsGenericType) {
				throw new ArgumentException("Must be a generic Type: " + type);
			}
			var collection = (IList)Activator.CreateInstance(type);
			type = type.GenericTypeArguments[0];
			var values = strValue.Split();
			foreach (var str in values) { //could do this generically, but most Collections also implement IList
				var item = type.Parse(str, cultureInfo);
				collection.Add(item);
			}
			return collection;
		}
	}
}