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
			public bool hidden { get; set; }

			public Subtree()
			{
				subgroups=new List<Subtree>();
				hidden = true;
			}
		}

		public Subtree Data { get; set; }


		public Checktree()
		{
			Data=new Subtree();
		}
	}
}