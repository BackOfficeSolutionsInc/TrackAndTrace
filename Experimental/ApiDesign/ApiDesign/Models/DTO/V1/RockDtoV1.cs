using ApiDesign.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ApiDesign.Models.DTO.V1 {

	public class RockDtoV1 : IDto<IRockModel> {
		public long Id { get; set; }		
		public string MyName { get; set; }
		public bool HasOwner { get; set; }
	}

	public class RockDtoV1_Converter : IDtoFactory<IRockModel>{
		public IDto<IRockModel> Convert(IRockModel model, IDtoFactoryHelper helper) {
			return new RockDtoV1() {
				Id = model.GetId(),
				MyName = model.GetName(),
				HasOwner = model.GetOwner()!=null,
			};
		}		

		public IEnumerable<int> ForVersions() {
			yield return Const.API.V1;
		}
	}
}