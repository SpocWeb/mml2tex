using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace org.SpocWeb.root.Data.xmls.MathML.Attributes {

	public class MColor : ITextWriteAble, IXmlSerializable {

		#region static Color Members

		public static MColor Black = new(0x000000);
		public static MColor Silver = new(0xc0c0c0);
		public static MColor Gray = new(0x808080);
		public static MColor White = new(0xffffff);

		public static MColor Red = new(0xff0000);
		public static MColor Green = new(0x00ff00);
		public static MColor Blue = new(0x0000ff);

		public static MColor Cyan = new(0x00ffff);
		public static MColor Magenta = new(0xff00ff);
		public static MColor Yellow = new(0xffff00);

		public static IReadOnlyDictionary<string, MColor> ColorByName => _ColorByName;
		static readonly Dictionary<string, MColor> _ColorByName = new();

		public static IReadOnlyDictionary<int, MColor> ColorByValue => _ColorByValue;
		static readonly Dictionary<int, MColor> _ColorByValue = new();

		static MColor() {
			foreach (var field in typeof(MColor).GetFields(BindingFlags.Static | BindingFlags.Public)) {
				if (field.FieldType != typeof(MColor)) {
					continue;
				}
				var namedColor = (MColor)field.GetValue(null);
				namedColor.Name = field.Name.ToLower();
				_ColorByName.Add(namedColor.Name, namedColor);
				_ColorByValue.Add(namedColor.RgbColor, namedColor);
			}
		}

		#endregion

		public MColor() { }

		public MColor(int rgbColor, string? name = null) {
			RgbColor = rgbColor;
			Name = name;
		}

		/// <summary> Optional Color Name </summary>
		public string Name { get; private set; }

		public int RgbColor { get; private set; }

		public long WriteTo(TextWriter writer, long lengthLeft) {
			if (!string.IsNullOrEmpty(Name)) {
				writer.Write(Name);
				return lengthLeft - Name.Length;
			}
			var color = RgbColor.ToString("x6");
			writer.Write('#');
			writer.Write(color);
			return lengthLeft - color.Length;
		}

		public XmlSchema GetSchema() => null;

		public void ReadXml(XmlReader reader) {
			if (reader.Value[0] == '#') {
				RgbColor = Convert.ToInt32(reader.Value.Substring(1), 16);
				return;
			}
			if (!ColorByName.TryGetValue(reader.Value, out var color)) {
				throw new ArgumentException("Could not find color: " + reader.Value);
			}
			Name = color.Name;
			RgbColor = color.RgbColor;
		}

		public override string ToString() {
			var sb = new StringWriter();
			WriteTo(sb, long.MaxValue);
			return sb.ToString();
		}

		public void WriteXml(XmlWriter writer) => writer.WriteString(ToString());
	}
}