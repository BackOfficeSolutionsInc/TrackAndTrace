using ApiDesign.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ApiDesign.Models.DTO.V2 {
	public class UserOrgDtoV2 : IDto<IUserOrganizationModel> {
		public long Id { get; set; }
		public string Name { get; set; }
		
		public class Converter : IDtoFactory<IUserOrganizationModel> {
			public IDto<IUserOrganizationModel> Convert(IUserOrganizationModel model, IDtoFactoryHelper helper) {
				return new UserOrgDtoV2() {
					Id = model.GetId(),
					Name  = model.GetName()
				};
			}
			public IEnumerable<int> ForVersions() {
				yield return Const.API.V2;
			}
		}
	}
}