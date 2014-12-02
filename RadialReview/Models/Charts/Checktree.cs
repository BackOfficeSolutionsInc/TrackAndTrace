using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Charts
{
	public class Checktree
	{
		public class Subtree
		{
			public List<Subtree> subgroups { get; set; }
			public string title { get; set; }
			public string id { get; set; }

			public Subtree()
			{
				subgroups=new List<Subtree>();
			}
		}

		public Subtree Data { get; set; }


		public Checktree()
		{
			Data=new Subtree();
		}
	}
}