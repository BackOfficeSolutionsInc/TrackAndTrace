using FluentNHibernate.Mapping;
using Newtonsoft.Json;
using NHibernate;
using RadialReview.Models.Application;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview {
	public class Variable {

		public static class Names {
			//Do not change these strings!! They are DB constants
			public static string LAST_CAMUNDA_UPDATE_TIME = "LAST_CAMUNDA_UPDATE_TIME";
			public static string USER_RADIAL_DATA_IDS = "USER_RADIAL_DATA_IDS";
			public static string CONSENT_MESSAGE = "CONSENT_MESSAGE";
			public static string PRIVACY_URL = "PRIVACY_URL";
			public static string TOS_URL = "TOS_URL";
			public static string DELINQUENT_MESSAGE_MEETING = "DELINQUENT_MESSAGE_MEETING";
			public static string UPDATE_CARD_SUBJECT = "UPDATE_CARD_SUBJECT";
			public static string TODO_DIVISOR = "TODO_DIVISOR";
			public static string INJECTED_SCRIPTS = "INJECTED_SCRIPTS";
			public static string LOG_ERRORS = "LOG_ERRORS";
			public static string LAYOUT_WEIGHTS = "LAYOUT_WEIGHTS";
			public static string LAYOUT_SETTINGS = "LAYOUT_SETTINGS";
		}

		public virtual string K { get; set; }
		public virtual string V { get; set; }
		public virtual DateTime LastUpdate { get; set; }

		public Variable() {
			LastUpdate = DateTime.UtcNow;
		}

		public class Map : ClassMap<Variable> {
			public Map() {
				Id(x => x.K).GeneratedBy.Assigned();
				Map(x => x.V).Length(1024);
				Map(x => x.LastUpdate);
			}
		}
	}

}
namespace RadialReview.Variables {
	
	public class VariableAccessor {
		public static string Get(string key, Func<string> defaultValue) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var v = s.GetSettingOrDefault(key, ()=>defaultValue());
					tx.Commit();
					s.Flush();
					return v;
				}
			}
		}
		public static T Get<T>(string key, Func<T> defaultValue) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var v = s.GetSettingOrDefault(key, defaultValue);
					tx.Commit();
					s.Flush();
					return v;
				}
			}
		}

		public static T Get<T>(string key, T defaultValue) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var v = s.GetSettingOrDefault(key,()=> defaultValue);
					tx.Commit();
					s.Flush();
					return v;
				}
			}
		}
	}

	public static class VariableExtensions {
		#region Session
		private static Variable _GetSettingOrDefault(this ISession s, string key, Func<string> defaultValue = null) {
			var found = s.Get<Variable>(key);
			if (found == null) {
				found = new Variable() {
					K = key,
					V = defaultValue == null ? null : defaultValue()
				};
				s.Save(found);
			}
			return found;
		}
		public static string GetSettingOrDefault(this ISession s, string key, Func<string> defaultValue = null) {
			return _GetSettingOrDefault(s, key, defaultValue).V;
		}
		public static string GetSettingOrDefault(this ISession s, string key, string defaultValue) {
			return _GetSettingOrDefault(s, key, () => defaultValue).V;
		}
		public static T GetSettingOrDefault<T>(this ISession s, string key, Func<T> defaultValue) {
			return JsonConvert.DeserializeObject<T>(_GetSettingOrDefault(s, key, () => JsonConvert.SerializeObject(defaultValue())).V);
		}
		public static T GetSettingOrDefault<T>(this ISession s, string key, T defaultValue) {
			return JsonConvert.DeserializeObject<T>(_GetSettingOrDefault(s, key, () => JsonConvert.SerializeObject(defaultValue)).V);
		}
		public static Variable UpdateSetting<T>(this ISession s, string key, T newValue) {
			return UpdateSetting(s, key, JsonConvert.SerializeObject(newValue));
		}
		public static Variable UpdateSetting(this ISession s, string key, string newValue) {
			var found = _GetSettingOrDefault(s, key, () => newValue);
			if (found.V != newValue) {
				found.V = newValue;
				found.LastUpdate = DateTime.UtcNow;
				s.Update(found);
			}
			return found;
		}
		#endregion
		#region StatelessSession
		private static Variable _GetSettingOrDefault(this IStatelessSession s, string key, Func<string> defaultValue = null) {
			var found = s.Get<Variable>(key);
			if (found == null) {
				found = new Variable() {
					K = key,
					V = defaultValue == null ? null : defaultValue()
				};
				s.Insert(found);
			}
			return found;
		}
		public static T GetSettingOrDefault<T>(this IStatelessSession s, string key, T defaultValue) {
			return JsonConvert.DeserializeObject<T>(_GetSettingOrDefault(s, key, () => JsonConvert.SerializeObject(defaultValue)).V);
		}
		#endregion
	}
}