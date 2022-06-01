using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace org.SpocWeb.root.Data.xmls.MathML {

	internal static class XProperty {

		public const string BracketOpen =  "\u2329{[(";
		public const string BracketClose = "\u232A}])";

		/// <summary> This doesn't guarantee proper Bracketing! Rather count and align Brackets! </summary>
		public static bool IsBracketed(this StringBuilder sb) {
			int pos = BracketOpen.IndexOf(sb[0]);
			return pos >= 0 && sb[sb.Length-1] == BracketClose[pos];
		}

		/// <summary> sets ALL Default Values of <paramref name="obj"/></summary>
		public static void SetDefaultValues(this object obj, bool setFieldDefaults = false) {
			var type = obj.GetType();
			if (setFieldDefaults) {
				foreach (var field in type.GetFields()) {
					var attrib = field.GetCustomAttribute<DefaultValueAttribute>();
					if (attrib is null) {
						continue;
					}
					var fallback = attrib.Value;
					field.SetValue(obj, Convert.ChangeType(fallback, field.FieldType));
				}
			}
			foreach (var property in type.GetProperties()) {
				var attrib = property.GetCustomAttribute<DefaultValueAttribute>();
				if (attrib is null || typeof(ICollection).IsAssignableFrom(property.PropertyType)) {
					continue;
				}
				var fallback = attrib.Value;
				property.SetValue(obj, Convert.ChangeType(fallback, property.PropertyType));
			}
		}

	}
}