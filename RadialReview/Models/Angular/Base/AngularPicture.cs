using RadialReview.Models.Askables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Angular.Base {
	public class AngularPicture : BaseAngular{

#pragma warning disable CS0618 // Type or member is obsolete
		public AngularPicture() { }
#pragma warning restore CS0618 // Type or member is obsolete
		public AngularPicture(long id) : base(id){
		}

		public AngularPicture(ResponsibilityGroupModel model) : base(model.Id) {
			ImageUrl = model.GetImageUrl();
			Name = model.GetName();
			Initials = "";
			if (model is UserOrganizationModel)
				Initials = ((UserOrganizationModel)model).GetInitials();
			
		}
		public string ImageUrl { get; set; }
		public string Name { get; set; }
		public string Initials { get; set; }
	}
}