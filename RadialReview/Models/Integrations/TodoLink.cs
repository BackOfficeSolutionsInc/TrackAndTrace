using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Integrations {

	public enum TodoService {
		Asana = 1,
	}

	public class TodoLink : ILongIdentifiable {
		public virtual long Id { get; set; }
		public virtual long InternalTodoId { get; set; }
		public virtual TodoService Service { get; set; }
		public virtual long ServiceTodoId { get; set; }

		public class Map : ClassMap<TodoLink> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.InternalTodoId);
				Map(x => x.Service).CustomType<TodoService>();
				Map(x => x.ServiceTodoId);
			}
		}
	}
}