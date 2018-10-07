using ApiDesign.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ApiDesign.Models.DTO.V2 {
	public class RockDtoV2 : IDto<IRockModel>, IDtoFactory<IRockModel> {

		public long Id { get; set; }
		public string Name { get; set; }
		public IDto<IUserOrganizationModel> Owner { get; set; }

		public IDto<IRockModel> Convert(IRockModel model, IDtoFactoryHelper helper) {
			return new RockDtoV2() {
				Id = model.GetId(),
				Name = model.GetName(),
				Owner = helper.Convert(model.GetOwner()),
			};
		}

		public IEnumerable<int> ForVersions() {
			yield return Const.API.V2;
		}
	}
}