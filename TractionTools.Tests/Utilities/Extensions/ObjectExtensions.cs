using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TractionTools.Tests.Reflections {
    public static class ReflectionExtensions {
        public static dynamic ToDynamic(this object value) {
            IDictionary<string, object> expando = new ExpandoObject();

            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(value.GetType()))
                expando.Add(property.Name, property.GetValue(value));

            return expando as ExpandoObject;
        }
        public static IDictionary<string, object> ToObjectDictionary(this object value) {
            IDictionary<string, object> expando = new ExpandoObject();

            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(value.GetType()))
                expando.Add(property.Name, property.GetValue(value));

            return expando;
        }

        /// <summary>
        /// Uses reflection to get the field value from an object.
        /// </summary>
        ///
        /// <param name="type">The instance type.</param>
        /// <param name="instance">The instance object.</param>
        /// <param name="fieldName">The field's name which is to be fetched.</param>
        ///
        /// <returns>The field value from the object.</returns>
        internal static object GetInstanceField<T>(this T instance, string fieldName) {
            var type = typeof(T);
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            return field.GetValue(instance);
        }
        internal static object GetInstanceProperty<T>(this T instance, string fieldName) {
            var type = typeof(T);
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            var property = type.GetProperty(fieldName, bindFlags);
            return property.GetValue(instance);
        }
        internal static object GetStaticField<T>(string fieldName) {
            Type type = typeof(T);
            FieldInfo info = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            return info.GetValue(null);
        }


    }
}
