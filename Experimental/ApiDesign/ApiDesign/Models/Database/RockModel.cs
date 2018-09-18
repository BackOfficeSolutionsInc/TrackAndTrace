using ApiDesign.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ApiDesign.Models.Database {
	public class RockModel : IRockModel {

		public long Id { get; set; }
		public string Rock { get; set; }
		public long OwnerId { get; set; }

		public long GetId() {
			return Id;
		}

		public string GetName() {
			return Rock;
		}

		public IUserOrganizationModel GetOwner() {
			return new UserOrganizationModel() {
				Id = OwnerId
			}; 
		}
	}
}