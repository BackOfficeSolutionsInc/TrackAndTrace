using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Angular
{
	public class AngularRock : Angular
	{
		public AngularRock(long id) : base(id){}
		public string Name { get; set; }
		public AngularUser Owner { get; set; }

	}
}