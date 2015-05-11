using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Models.Angular.Users
{
	public class AngularUser : Base.BaseAngular
	{
		public AngularUser(UserOrganizationModel user,ImageSize imageSize = ImageSize._64) : base(user.Id)
		{
			Name = user.GetName();
			ImageUrl = user.ImageUrl(true, imageSize);
			var inits = new List<string>();
			if (user.GetFirstName() != null && user.GetFirstName().Length > 0)
				inits.Add(user.GetFirstName().Substring(0, 1));
			if (user.GetLastName() != null && user.GetLastName().Length > 0)
				inits.Add(user.GetLastName().Substring(0,1));
			Initials = string.Join(" ",inits).ToUpperInvariant();
		}

		public AngularUser(long id) : base(id) { }

		public AngularUser() { }

		public string Name { get; set; }
		public string ImageUrl { get; set; }
		public string Initials { get; set; }
	}
}