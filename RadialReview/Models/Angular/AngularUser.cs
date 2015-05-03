using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Angular
{
	public class AngularUser : Angular
	{
		public AngularUser(long id) : base(id){}

		public string Name { get; set; }
		public string ImageUrl { get; set; }
	}
}