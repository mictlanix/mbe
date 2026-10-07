//
// IncidenceContent.cs
//
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
//
// Copyright (C) 2026 Eddy Zavaleta, Mictlanix, and contributors.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mictlanix.BE.Web.Models {
	// incidence.content holds whatever the writer put there: a JSON snapshot of the
	// record (payments, prices, customers), a sentence ("Product created."), a user
	// name, or nothing. This turns it into either a field/value list or plain text.
	//
	// A snapshot is the serialized model, so each field is labelled and formatted
	// through the model property it came from: its [Display] name, enum display
	// names, the app's date format.
	public class IncidenceContent {
		public string Text { get; private set; }
		public IList<KeyValuePair<string, string>> Fields { get; private set; }

		// True for a payment correction written before mictlanix/mbe#42, whose
		// "previous" state is really the state after the change.
		public bool IsPostChangeSnapshot { get; private set; }

		public bool IsStructured {
			get { return Fields != null; }
		}

		public static IncidenceContent Parse (Incidence incidence)
		{
			var item = new IncidenceContent ();
			var content = incidence.PreviousState;
			var json = TryParseObject (content);

			if (json == null) {
				item.Text = content;
				return item;
			}

			var type = ModelType (incidence.SourceType);

			item.Fields = new List<KeyValuePair<string, string>> ();

			foreach (var field in json.Properties ()) {
				var property = type == null ? null : type.GetProperty (field.Name);
				var value = Describe (field.Value, property);

				if (!string.IsNullOrEmpty (value)) {
					item.Fields.Add (new KeyValuePair<string, string> (Label (field.Name, property), value));
				}
			}

			item.IsPostChangeSnapshot = incidence.SourceType == SourceType.CustomerPayment &&
				IsPostChange (json);

			return item;
		}

		static JObject TryParseObject (string content)
		{
			if (string.IsNullOrWhiteSpace (content) || !content.TrimStart ().StartsWith ("{")) {
				return null;
			}

			try {
				using (var reader = new JsonTextReader (new System.IO.StringReader (content))) {
					// Keep dates as the strings that were written.
					reader.DateParseHandling = DateParseHandling.None;
					return JObject.Load (reader);
				}
			} catch (JsonReaderException) {
				return null;
			}
		}

		// The model each source's snapshot was serialized from. Price changes
		// snapshot the product.
		static Type ModelType (SourceType source)
		{
			var name = source == SourceType.Pricing ? "Product" : source.ToString ();
			return typeof (Incidence).Assembly.GetType (typeof (Incidence).Namespace + "." + name);
		}

		static string Label (string field, PropertyInfo property)
		{
			var display = property == null ? null : property.GetCustomAttribute<DisplayAttribute> ();

			if (display != null && display.ResourceType != null) {
				return display.GetName ();
			}

			return Resources.ResourceManager.GetString (field) ?? field;
		}

		// Nested records are shown by name, collections by size.
		static string Describe (JToken token, PropertyInfo property)
		{
			var type = property == null ? null : Nullable.GetUnderlyingType (property.PropertyType) ?? property.PropertyType;

			switch (token.Type) {
			case JTokenType.Null:
			case JTokenType.Undefined:
				return null;
			case JTokenType.Object:
				var name = token ["Name"] ?? token ["Code"] ?? token ["Id"];
				return name == null || name.Type == JTokenType.Null ? null : name.ToString ();
			case JTokenType.Array:
				var count = ((JArray) token).Count;
				return count == 0 ? null : count.ToString ();
			case JTokenType.Boolean:
				return (bool) token ? Resources.Yes : Resources.No;
			case JTokenType.Integer:
				if (type != null && type.IsEnum) {
					var value = Enum.ToObject (type, (long) token);
					return Enum.IsDefined (type, value) ? ((Enum) value).GetDisplayName () : token.ToString ();
				}
				return token.ToString ();
			case JTokenType.String:
				DateTimeOffset date;
				if (type == typeof (DateTime) &&
				    DateTimeOffset.TryParse ((string) token, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date)) {
					// The wall-clock time as written, whatever offset it carried.
					return date.DateTime.ToString (Resources.DateTimeFormatString);
				}
				return (string) token;
			default:
				return token.ToString ();
			}
		}

		// Payments can be corrected once, so the true previous state of a correction
		// was never modified: its ModificationTime equals its CreationTime. Before
		// mictlanix/mbe#42 the snapshot was taken after ModificationTime had been set
		// to the time of the correction. Compared to the second, ignoring any offset.
		static bool IsPostChange (JObject json)
		{
			var created = (string) json ["CreationTime"];
			var modified = (string) json ["ModificationTime"];

			if (created == null || modified == null || created.Length < 19 || modified.Length < 19) {
				return false;
			}

			return created.Substring (0, 19) != modified.Substring (0, 19);
		}
	}
}
