using ApiDesign.Models.Database;
using ApiDesign.Models.DTO;
using ApiDesign.Models.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ApiDesign.Utilites.DTO {
	public class DtoSerializer : JsonConverter {
		public DtoSerializer() {
		}

		public DtoSerializer(int? version) {
			Version = version;
		}

		public int? Version { get; set; }

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {

			if (value is IBackend) {
				try {
					var version = Version ?? int.Parse((string)HttpContext.Current.Request.RequestContext.RouteData.Values["version"]);
					value = DtoConverter.ConvertFromBackendModel(version, value);
				} catch (DtoConverterException) {
					throw;
				} catch (Exception) {
					throw new Exception("Cannot return backend objects");
				}
			}

			serializer.Serialize(writer, value);

			//PriceHistoryRecordModel obj = value as PriceHistoryRecordModel;
			//JToken t = JToken.FromObject(new double[] { obj.Date.Ticks, obj.Value });
			//t.WriteTo(writer);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
			throw new NotImplementedException();
		}

		public override bool CanConvert(Type objectType) {
			return typeof(IBackend).IsAssignableFrom(objectType) ;
		}
	}
}