using System.ComponentModel;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using org.SpocWeb.root.Data.xmls.MathML.Attributes;

namespace org.SpocWeb.root.Data.xmls.MathML;

public abstract class MElement : AMathMlNode {

	public const sbyte ImpliedMRowArity = 1; //sbyte.MaxValue; Schema allows for many!

	/// <summary> Used by MS Word to indicate the Start of the Sum-Arg; implemented by (invisible) <see cref="Elements.MFenced"/> Element around Arg </summary>
	public const string OpGlueNAry = ""; //"▒";
	public const char OpEquationArray = '█';

	protected MElement() {
		this.SetDefaultValues();
	}

	[DefaultValue("")]
	public string Id { get; set; }

	/// <summary> CSS Class </summary>
	[DefaultValue("")]
	public string Class { get; set; }

	/// <summary> CSS Style </summary>
	[DefaultValue("")]
	public string Style { get; set; }

	[DefaultValue("")]
	public string HRef { get; set; }

	public MColor MathBackground { get; set; }

	public MColor MathColor { get; set; }

	//public double FontSize { get; set; }
	//public string FontFamily { get; set; }
	//public string FontStyle { get; set; }
	//public string FontWeight { get; set; }

	//public MColor Background { get; set; }
	//public MColor Color { get; set; }


	[DefaultValue(MMathVariant.Normal)]
	public MMathVariant MathVariant { get; set; }

	public override void WriteXml(XmlWriter writer) => this.WriteTo(writer);

	static readonly Dictionary<string, Type> TypesByName = new();
	static MElement() {
		var ass = Assembly.GetExecutingAssembly();
		foreach (var type in ass.ExportedTypes) {
			if (!typeof(AMathMlNode).IsAssignableFrom(type)) {
				continue;
			}
			var attrib = type.GetCustomAttribute<XmlRootAttribute>();
			TypesByName[attrib is not null ? attrib.ElementName : type.Name.ToLower()] = type;
		}
	}

	public override void ReadXml(XmlReader reader) {
		reader.ReadAttributes(this);
		while (reader.Read()) { // ReSharper disable once SwitchStatementMissingSomeCases
			switch (reader.NodeType) {
				case XmlNodeType.EndElement: 
					if (Children.Count < Arity && 0 
					    != (reader.Settings?.ValidationFlags & XmlSchemaValidationFlags.ReportValidationWarnings ?? 0)) {
						throw new XmlException("Too few Children: " + Children.Count);
					}
					return;
				case XmlNodeType.CDATA: 
				//case XmlNodeType.EntityReference: 
				//case XmlNodeType.Entity: 
				//case XmlNodeType.EndEntity: 
				case XmlNodeType.SignificantWhitespace: 
				case XmlNodeType.Text: Children.Add(new MathMlTextNode(reader.Value)); continue;
				case XmlNodeType.Whitespace: 
				case XmlNodeType.Comment: continue;
				case XmlNodeType.Element: break; 
				default: throw new ArgumentOutOfRangeException();
			}
			if (!TypesByName.TryGetValue(reader.Name, out var typ)) {
				if (0 != (reader.Settings?.ValidationFlags & XmlSchemaValidationFlags.ReportValidationWarnings)) {
					throw new XmlException("Unexpected Node Name: " + reader.Name);
				} //skip the whole subnode
				reader.ReadElementContentAsString();
				//reader.ReadElementContentAs(typ, reader.Settings.NameTable);
				//reader.ReadSubtree();
				continue;
			}
			var obj = Activator.CreateInstance(typ);
			var child = (AMathMlNode)obj;
			if (ChildType is not null &&
			    ChildType != typ && 0 
			    != (reader.Settings?.ValidationFlags & XmlSchemaValidationFlags.ReportValidationWarnings)) {
				throw new XmlException("Child must be of Type " + ChildType);
			}
			//var validationMessage = IsValidChild(child);
			//if (validationMessage.Length > 0 && 0 
			//	!= (reader.Settings?.ValidationFlags & XmlSchemaValidationFlags.ReportValidationWarnings)) {
			//	throw new XmlException(validationMessage);
			//}
			Children.Add(child);
			if (Children.Count > Arity && 0 
			    != (reader.Settings?.ValidationFlags & XmlSchemaValidationFlags.ReportValidationWarnings ?? 0)) {
				throw new XmlException("Too many Children: " + Children.Count);
			}
			if (!reader.IsEmptyElement) {
				child.ReadXml(reader);
			}
		}
	}

	[XmlIgnore] public virtual Type ChildType => null; 

	//protected virtual string IsValidChild(AMathMlNode child) => "";

	/// <summary> The # <see cref="Children"/> of this Node </summary>
	[XmlIgnore] public abstract sbyte Arity { get; }

	[XmlIgnore] public List<AMathMlNode> Children { get; set; } = new();

	internal override AsciiMath.Parser.AmSymbol GetSymbol() => Children.Count == 1 ? Children[0].GetSymbol() : null;

	public override TextWriter AppendTo(TextWriter sb, bool toAscii) {
		var numAttribs = AppendAttributesTo(sb);
		AppendChildren(sb, toAscii);
		for (int i = numAttribs; --i >= 0; ) {
			//sb.Write('}');
		}
		return sb;
	}

	int AppendAttributesTo(TextWriter writer) {
		var element = this;
		Type type = element.GetType();
		var attributes = type.GetAttributes(element);
		int ret = 0;
		foreach (var attribute in attributes) {
			var name = attribute.Item1; // ReSharper disable once SwitchStatementMissingSomeCases
			switch (name) {
				case AsciiMath.Parser.ATTRIB_MATH_VARIANT: //nameof(MathVariant)) {
					var prefix = MathVariant.Prefix();
					writer.Write(prefix);
					writer.Write(' ');
					ret += prefix.Count(c => c == '{');
					continue;
				case AsciiMath.Parser.ATTRIB_MATH_COLOR: name = AsciiMath.Parser.STR_COLOR; break;
			}
			writer.Write(name);
			writer.Write('['); //use ( for User-Brackets, { for Grouping and [ for Attributes
			writer.Write(attribute.Item3);
			writer.Write(']');
			//writer.Write('{');
			++ret;
		}
		return ret;
	}

	protected string _Open = "{";
	protected string _Separators = " ";
	protected string _Close = "}";

	void AppendChildren(TextWriter sb, bool toAscii) {
		var last = Children.Count - 1;
		if (last < 0) {
			return;
		}
		if (last <= 0) {
			Children[last].AppendTo(sb, toAscii);
			return;
		}
		sb.Write(_Open);
		for (var i = 0; i < last; i++) {
			Children[i].AppendTo(sb, toAscii).Write(_Separators);
		}
		//var isBracketed = sb
		Children[last].AppendTo(sb, toAscii).Write(_Close);
	}

}