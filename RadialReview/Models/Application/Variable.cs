using FluentNHibernate.Mapping;
using Newtonsoft.Json;
using NHibernate;
using RadialReview.Models.Application;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Application {
	public class Variable {

		public static class Names {
			public static string LAST_CAMUNDA_UPDATE_TIME = "LAST_CAMUNDA_UPDATE_TIME";
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
	}

    public static class VariableExtensions{

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
		
        public static void UpdateSetting<T>(this ISession s, string key, T newValue) {
            UpdateSetting(s, key, JsonConvert.SerializeObject(newValue));
        }

        public static void UpdateSetting(this ISession s, string key, string newValue) {
            var found = _GetSettingOrDefault(s, key, () => newValue);
            if (found.V != newValue) {
                found.V = newValue;
                found.LastUpdate = DateTime.UtcNow;
                s.Update(found);
            }
        }
    }
}