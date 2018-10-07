using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiDesign.Models.Interfaces {
	public interface IRockModel : IBackend<IRockModel>, INameableModel {
		IUserOrganizationModel GetOwner();
	}
}
