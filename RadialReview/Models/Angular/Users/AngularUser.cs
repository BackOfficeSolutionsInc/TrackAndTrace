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
			Initials = user.GetInitials();
			
		}

		public static AngularUser NoUser()
		{
			return new AngularUser(-1){
				Name ="n/a",
				ImageUrl = null,
				Initials = "n/a"
			};
		}

		public AngularUser(long id) : base(id) { }

		public AngularUser() { }

		public string Name { get; set; }
		public string ImageUrl { get; set; }
		public string Initials { get; set; }
	}
}