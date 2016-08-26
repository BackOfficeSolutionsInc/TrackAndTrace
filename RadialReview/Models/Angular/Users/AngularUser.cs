﻿using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Proxy;

namespace RadialReview.Models.Angular.Users
{
	public class AngularUser : Base.BaseAngular
	{		
		public AngularUser(long id) : base(id)
		{
			
		}

		public static AngularUser CreateUser(UserOrganizationModel user, ImageSize imageSize = ImageSize._64, bool? managing = null)
		{
			if (user == null)
				return NoUser();


			return new AngularUser(user.Id){
				Name = user.GetName(),
				ImageUrl = user.ImageUrl(true, imageSize),
				Initials = user.GetInitials(),
				Managing = managing,
                CreateTime = user.CreateTime,
			};
		}
		public static AngularUser CreateUser(UserModels.UserLookup user,ImageSize imageSize = ImageSize._64,bool? managing = null)
		{
			if (user == null)
				return NoUser();

			return new AngularUser(user.UserId)
			{
				Name = user.Name,
				ImageUrl = user.ImageUrl(imageSize),
				Initials = user.GetInitials(),
				Managing = managing
			};
		}


		public static AngularUser NoUser()
		{
			return new AngularUser(-1){
				Name ="n/a",
				ImageUrl = null,
				Initials = "n/a",
				Managing = null,
                
			};
		}

#pragma warning disable CS0618 // Type or member is obsolete
		public AngularUser() { }
#pragma warning restore CS0618 // Type or member is obsolete

		public string Name { get; set; }
		public string ImageUrl { get; set; }
		public string Initials { get; set; }
		public bool? Managing { get; set; }
        public DateTime? CreateTime { get;set;}
	
	}
}