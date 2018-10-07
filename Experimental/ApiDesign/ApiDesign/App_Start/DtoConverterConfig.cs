using ApiDesign.Models.DTO.V1;
using ApiDesign.Models.DTO.V2;
using ApiDesign.Utilites.DTO;
using System;
using System.Web.Http;

namespace ApiDesign {
	public class DtoConverterConfig {
		public static void Register(HttpConfiguration config) {

			DtoConverter.RegisterConverter(new RockDtoV1_Converter());
			DtoConverter.RegisterConverter(new RockDtoV2());
			DtoConverter.RegisterConverter(new UserOrgDtoV2.Converter());


		}
	}
}