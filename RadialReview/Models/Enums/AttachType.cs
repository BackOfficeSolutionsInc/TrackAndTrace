using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Enums {

	public class Attach {
		public AttachType Type { get; set; }
		public long Id { get; set; }
		public string Name { get; set; }

		public Attach() {
		}

		public Attach(AttachType type,long id,string name = null) {
			Type = type;
			Id = id;
			Name = name;
		}
	}
	
	public enum AttachType {
		Invalid = 0,
		Position = 1,
		Team = 2,
		User = 3,

		MAX = 100
	}
}