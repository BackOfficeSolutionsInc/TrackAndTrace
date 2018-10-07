using ApiDesign.Models.DTO;
using ApiDesign.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

namespace ApiDesign.Utilites.DTO {
	public class DtoConverter {

		private static Dictionary<Type, Dictionary<int, IDtoFactory>> Converters = new Dictionary<Type, Dictionary<int, IDtoFactory>>();

		private DtoConverter() {
			//Singleton
		}
		/// <summary>
		/// Register the converter with WebApi.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="v"></param>
		/// <param name="modelFactory"></param>
		public static void RegisterConverter<T>(IDtoFactory<T> modelFactory) where T : IBackend<T> {
			if (modelFactory == null)
				throw new ArgumentNullException(nameof(modelFactory));

			var type = typeof(T);
			if (!Converters.ContainsKey(type)) {
				Converters[type] = new Dictionary<int, IDtoFactory>();
			}

			var versions = modelFactory.ForVersions();
			if (versions == null)
				throw new ArgumentException("DtoModelFactory must have a non-null version number");

			foreach (var v in versions) {
				if (Converters[type].ContainsKey(v)) {
					throw new ArgumentException("DtoModelFactory already registered (" + type + ") [" + v + "]");
				}
				Converters[type][v] = modelFactory;
			}
		}
		/// <summary>
		/// Can return null.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="v"></param>
		/// <param name="modelFactory"></param>
		/// <returns>The converter if one exists, otherwise null.</returns>
		private static IDtoFactory GetConverter(int v, Type type) {
			if (!Converters.ContainsKey(type)) {
				throw new DtoConverterDoesNotExist("Converter for Type was not registered (" + type.FullName + ") [" + v + "]");
			}
			if (!Converters[type].ContainsKey(v)) {
				throw new DtoConverterDoesNotExist("Converter for Version was not registered (" + type.FullName + ") [" + v + "]");
			}
			return Converters[type][v] as IDtoFactory;
		}

		public static IDto ConvertFromBackendModel(int version, Type type, object model) {
			var converter = GetConverter(version, type);
			var method = converter.GetType().GetMethod("Convert");
			return (IDto)method.Invoke(converter, new[] { model, new DtoFactoryHelper(version) });
		}

		public static IDto ConvertFromBackendModel(int version, object model) {
			var backendTypes = model.GetType().GetInterfaces()
				.Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IBackend<>))
				.ToList();

			if (backendTypes.Count() > 1)
				throw new DtoConverterException("Model implements IBackend<T> more than once");
			if (!backendTypes.Any())
				throw new DtoConverterException("Model does not implement IBackend<T>");

			var backendType = backendTypes.SingleOrDefault().GetGenericArguments()[0];

			return (IDto)ConvertFromBackendModel(version, backendType, model);
		}

		public static IDto<T> Convert<T>(int version, IBackend<T> model) where T : IBackend<T> {
			return (IDto<T>)ConvertFromBackendModel(version, typeof(T), model);
		}
		
		private class DtoFactoryHelper : IDtoFactoryHelper {
			public int Version { get; private set; }
			public DtoFactoryHelper(int v) {
				Version = v;
			}
			public IDto<T> Convert<T>(IBackend<T> submodel) where T : IBackend<T> {
				return DtoConverter.Convert(Version, submodel);
			}
		}


	}
	public class DtoConverterException : Exception {
		public DtoConverterException() { }
		public DtoConverterException(string message) : base(message) { }
		public DtoConverterException(string message, Exception inner) : base(message, inner) { }
	}
	public class DtoConverterDoesNotExist : DtoConverterException {
		public DtoConverterDoesNotExist() { }
		public DtoConverterDoesNotExist(string message) : base(message) { }
		public DtoConverterDoesNotExist(string message, Exception inner) : base(message, inner) { }
	}
}