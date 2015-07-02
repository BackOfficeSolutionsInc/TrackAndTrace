using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Models.Angular.Users
{
	public class AngularUser : Base.BaseAngular
	{		
		[Obsolete("User Static constructor",false)]
		public AngularUser(long id) : base(id)
		{
			
		}

		public static AngularUser CreateUser(UserOrganizationModel user,ImageSize imageSize = ImageSize._64)
		{
			if (user == null)
				return NoUser();

			return new AngularUser(user.Id){
				Name = user.NotNull(x => x.GetName()),
				ImageUrl = user.NotNull(x => x.ImageUrl(true, imageSize)),
				Initials = user.NotNull(x => x.GetInitials()),
			};

		}

		public static AngularUser NoUser()
		{
			return new AngularUser(-1){
				Name ="n/a",
				ImageUrl = null,
				Initials = "n/a"
			};
		}
		
		public AngularUser() { }

		public string Name { get; set; }
		public string ImageUrl { get; set; }
		public string Initials { get; set; }
	}
}