using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using NHibernate;
using NHibernate.Persister.Entity;
using System.Collections;
using NHibernate.Proxy;

namespace RadialReview.Utilities {
	/// <summary>
	/// NHibernate helper class
	/// </summary>
	/// <remarks>
	/// Assumes you are using NHibernate version 3.1.0.4000 or greater (Not tested on previous versions)
	/// </remarks>
	public class NHibernateHelper {

		/// <summary>
		/// Creates a dictionary of property and database column/field name given an
		/// NHibernate mapped entity
		/// </summary>
		/// <remarks>
		/// This method uses reflection to obtain an NHibernate internal private dictionary.
		/// This is the easiest method I know that will also work with entitys that have mapped components.
		/// </remarks>
		/// <param name="sessionFactory">NHibernate SessionFactory</param>
		/// <param name="type">An mapped type</param>
		/// <returns>Entity Property/Database column dictionary</returns>
		public static Dictionary<string, string> GetPropertyAndColumnNames(ISessionFactory sessionFactory, Type type) {
			// Get the objects type
			//Type type = entity.GetType();

			// Get the entity's NHibernate metadata
			var metaData = sessionFactory.GetClassMetadata(type.ToString());

			// Gets the entity's persister
			var persister = (AbstractEntityPersister)metaData;

			// Creating our own Dictionary<Entity property name, Database column/filed name>()
			var d = new Dictionary<string, string>();

			// Get the entity's identifier
			var entityIdentifier = metaData.IdentifierPropertyName;

			// Get the database identifier
			// Note: We are only getting the first key column.
			// Adjust this code to your needs if you are using composite keys!
			var databaseIdentifier = persister.KeyColumnNames[0];

			// Adding the identifier as the first entry
			d.Add(entityIdentifier, databaseIdentifier);

			// Using reflection to get a private field on the AbstractEntityPersister class
			var fieldInfo = typeof(AbstractEntityPersister).GetField("subclassPropertyColumnNames", BindingFlags.NonPublic | BindingFlags.Instance);

			// This internal NHibernate dictionary contains the entity property name as a key and
			// database column/field name as the value
			var pairs = (Dictionary<string, string[]>)fieldInfo.GetValue(persister);

			foreach (var pair in pairs) {
				if (pair.Value.Length > 0) {
					// The database identifier typically appears more than once in the NHibernate dictionary
					// so we are just filtering it out since we have already added it to our own dictionary
					if (pair.Value[0] == databaseIdentifier)
						break;

					d.Add(pair.Key, pair.Value[0]);
				}
			}

			return d;
		}

		//public class NHibernateProxyRemover {
		//	public static T Deproxy<T>(T objeto) {
		//		var type = typeof(T);
		//		//var retorno = Activator.CreateInstance(type);




		//		foreach (var propertyInfo in type.GetProperties()) {
		//		//	if ((IsEnum(propertyInfo) && IsPrimitive(propertyInfo)) || IsString(propertyInfo))
		//		//		continue;

		//			var value = propertyInfo.GetValue(objeto, null);
		//			if (value == null)
		//				continue;
		//			if (value is INHibernateProxy)
		//				propertyInfo.SetValue(objeto, null);

		//			if (value is IEnumerable) {

		//			}

		//			if (value is object) {
		//				Deproxy(value);
		//			}

		//			var valueType = value.GetType();

		//			if (ImplementsIEnumerable(propertyInfo)) {
		//				var genericArgument = propertyInfo.PropertyType.GetGenericArguments()[0];
		//				var type1 = typeof(List<>);
		//				var makeGenericType = type1.MakeGenericType(propertyInfo.PropertyType.GetGenericArguments());
		//				var instance = (IList)Activator.CreateInstance(makeGenericType);
		//				foreach (var obj in (IEnumerable)value) {
		//					var desproxyaONego = Desproxiador(obj, genericArgument);
		//					instance.Add(desproxyaONego);
		//				}
		//				propertyInfo.SetValue(retorno, instance, null);
		//				continue;
		//			}

		//			if (valueType.Name.Contains("Proxy")) {
		//				var desproxyaONego = Desproxiador(value, valueType.BaseType);
		//				propertyInfo.SetValue(retorno, desproxyaONego, null);
		//			}
		//		}

		//		return (T)retorno;
		//	}

		//	private static bool ImplementsIEnumerable(PropertyInfo propertyInfo) {
		//		return propertyInfo.PropertyType.GetInterface("IEnumerable") != null;
		//	}

		//	private static bool IsString(PropertyInfo propertyInfo) {
		//		return propertyInfo.PropertyType == typeof(string);
		//	}

		//	private static bool IsPrimitive(PropertyInfo propertyInfo) {
		//		return propertyInfo.PropertyType.IsPrimitive;
		//	}

		//	private static bool IsEnum(PropertyInfo propertyInfo) {
		//		return propertyInfo.PropertyType.IsEnum;
		//	}

		//	private static object Desproxiador(object objeto, Type baseType) {
		//		var retornavel = Activator.CreateInstance(baseType);

		//		foreach (var propertyInfo in baseType.GetProperties()) {
		//			var value = propertyInfo.GetValue(objeto, null);
		//			if (value == null)
		//				continue;
		//			var valueType = value.GetType();

		//			if (valueType.Name.Contains("Proxy"))
		//				value = Desproxiador(value, valueType.BaseType);

		//			propertyInfo.SetValue(retornavel, value, null);
		//		}

		//		return retornavel;
		//	}
		//}
	}
}