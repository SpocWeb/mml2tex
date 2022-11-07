using System.Xml.Serialization;
using org.SpocWeb.root.Data.xmls.MathML.Attributes;

namespace org.SpocWeb.root.Data.xmls.MathML.Elements;

/// <summary> Separator between and PreScript Pairs in <see cref="MMultiScripts"/> </summary>
public class MPreScripts : MElement { //,}AMathMlNode {
	public override sbyte Arity => 0;
}

/// <summary> used to place tensor indicies to all 4 Corners of a base expression. </summary>
/// <remarks>
/// used to put multiple columns of scripts on a base. 
/// It can even attach columns of "prescripts", following an <see cref="MPreScripts"/> Element, to a base. 
/// 
/// </remarks>
public class MMultiScripts : MElement {

	public MMeasuredLength SubScriptShift { get; set; }

	public MMeasuredLength SuperScriptShift { get; set; }

	/// <summary> Any Number of further <see cref="MMultiScripts"/> Expressions </summary>
	[XmlIgnore] public override sbyte Arity => sbyte.MaxValue;

	//[XmlIgnore] public override Type ChildType => typeof(MMultiScripts);

	public AMathMlNode Base => Children[0];

	public MPreScripts Sep => Children.OfType<MPreScripts>().FirstOrDefault();

	public IEnumerable<AMathMlNode> PostScripts => Children.Skip(1).TakeWhile(n => !(n is MPreScripts));

	public IEnumerable<AMathMlNode> PreScripts => Children.SkipWhile(n => !(n is MPreScripts));

}