using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Script.Serialization;

namespace RadialReview.Models.Frontend {

	public enum EditorType {
		text,
		textarea,
		checkbox,
		radio,
		span,
		div,
		header,
		h1, h2, h3, h4, h5, h6,
		number,
		date, datetime, time,
		file, yesno,
		label,
		select,
		hidden,
		@readonly,
		subform,
	}
	

	public class EditorForm {
		public IEnumerable<EditorField> fields { get; set; }
		public bool isEditorForm { get { return true; } }
		public string title { get; set; }

		public EditorForm() {
			title = "";
		}
	}

	public class EditorSubForm : EditorField {
		public Dictionary<string,IEnumerable<EditorField>> subforms { get; set; }
		public EditorSubForm(string name, string label = null, string value = null) : base(EditorType.subform, name, label, value) {
			subforms = new Dictionary<string, IEnumerable<EditorField>>();
		}

		public void AddSubForm(string name,string value, IEnumerable<EditorField> fields) {
			if (subforms.ContainsKey(name))
				throw new Exception("Subforms already contains key:"+name);
			subforms[name] = fields.ToList();
		}
	}

	public class EditorField {
		public class textvalue{
			public string text { get; set; }
			public string value { get; set; }

		}
		public string name { get; set; }
		public string text { get; set; }
		[ScriptIgnore]
		public EditorType editorType { get; set; }
		public string type { get { return editorType.ToString(); } }
		public string value { get; set; }
		public string placeholder { get; set; }
		public string help { get; set; }
		public List<textvalue> options { get; set; }

		public EditorField(EditorType type, string name, string label = null, string value = null) {
			this.editorType = type;
			text = label;
			this.name = name;
			this.value = value;
		}

		public EditorField AddOption(string key, string value) {
			if (options == null) {
				options = new List<textvalue>();
			}
			options.Add(new textvalue { text = key, value = value });
			return this;
		}
		public EditorField AddOptions<T>(IEnumerable<KeyValuePair<string, string>> items) {
			return AddOptions(items, x => x.Key, x => x.Value);
		}
		public EditorField AddOptions<T>(IEnumerable<KeyValuePair<string, long>> items) {
			return AddOptions(items, x => x.Key, x => "" + x.Value);
		}

		public EditorField AddOptions<T>(IEnumerable<T> items, Func<T, string> key, Func<T, string> value) {
			foreach (var i in items) {
				AddOption(key(i), value(i));
			}
			return this;
		}

		public EditorField DefaultValue(string value) {
			this.value = value;
			return this;
		}
		public EditorField DefaultValue(decimal value) {
			this.value = "" + value;
			return this;
		}
		public EditorField DefaultValue(int value) {
			this.value = "" + value;
			return this;
		}
		public EditorField DefaultValue(double value) {
			this.value = "" + value;
			return this;
		}

		public static EditorField Hidden(string name, string value) {
			return new EditorField(EditorType.hidden, name, value: value);
		}

		public static EditorField DropdownFromProperty<T, TProp, O>(T model, Expression<Func<T, TProp>> property, IEnumerable<KeyValuePair<string, O>> items) {
			return DropdownFromProperty(model, property, items, x => x.Key, x => "" + x.Value);
		}

		public static EditorField DropdownFromProperty<T, TProp, I>(T model, Expression<Func<T, TProp>> property, IEnumerable<I> items, Func<I, string> key, Func<I, string> value) {
			var editor = FromProperty(model, property);
			editor.editorType = EditorType.select;
			editor.options = null;
			editor.AddOptions(items, key, value);
			return editor;
		}

		public static EditorField FromProperty<T, TProp>(T model, Expression<Func<T, TProp>> property) {
			var value = property.Compile()(model);
			var name = property.GetMemberName();
			var mtype = property.GetMemberType();
			var type = GetEditorTypeFromType(mtype);
			var displayName = property.GetPropertyDisplayName();
			var placeholder = property.GetPropertyDisplayPrompt();
			var help = property.GetPropertyDisplayDescription();
			var options = new List<KeyValuePair<string, string>>();



			var editor = new EditorField(type ?? EditorType.text, name, label: displayName, value: (type == null ? "" : ("" + value))) {
				placeholder = placeholder,
				help = help
			};

			if (mtype.IsEnum) {
				editor.AddOptions(SelectExtensions.ToSelectList(mtype, "" + value), x => x.Text, x => x.Value);
			}
			return editor;
		}


		public static EditorType? GetEditorTypeFromType(Type type) {

			var numbers = new[] { typeof(int), typeof(long), typeof(double), typeof(decimal), typeof(int?), typeof(long?), typeof(double?), typeof(decimal?) };
			var checkbox = new[] { typeof(bool), typeof(bool?) };
			var text = new[] { typeof(string) };

			if (type.IsEnum)
				return EditorType.select;
			if (numbers.Any(x => x.IsAssignableFrom(type)))
				return EditorType.number;
			if (text.Any(x => x.IsAssignableFrom(type)))
				return EditorType.text;
			if (checkbox.Any(x => x.IsAssignableFrom(type)))
				return EditorType.checkbox;


			return null;
		}



	}
}