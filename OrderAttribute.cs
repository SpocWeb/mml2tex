namespace org.SpocWeb.root.Data.xmls.MathML {

	[AttributeUsage(AttributeTargets.Property)]
	public class OrderAttribute : Attribute {

		public OrderAttribute(int orderIndex) {
			OrderIndex = orderIndex;
		}

		public int OrderIndex { get; }
	}
}