namespace MathMl;

[AttributeUsage(AttributeTargets.Property)]
public class DefaultValueAttribute : Attribute {

	public DefaultValueAttribute(object value) {
		Value = value;
	}

	public object Value { get; }
}