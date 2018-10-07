using ApiDesign.Models.DTO;
using ApiDesign.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ApiDesign.Models.Database {
	public class UserOrganizationModel : IUserOrganizationModel {
		public long Id { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }


		public long GetId() {
			return Id;
		}

		public string GetName() {
			return (FirstName + " " + LastName).Trim();
		}

	}
}