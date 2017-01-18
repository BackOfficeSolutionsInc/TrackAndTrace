using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.QuestionModels {
	public class AskableSectionModel : ILongIdentifiable, IHistorical{

		public virtual long Id { get; set; }

		public virtual DateTime CreateTime { get; set; }

		public virtual DateTime? DeleteTime { get; set; }
		public virtual String Name { get; set; }
		public virtual String Color { get; set; }

		public virtual long OrganizationId { get; set; }

		public AskableSectionModel() {
		}

		public class Map : ClassMap<AskableSectionModel> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.Name);
				Map(x => x.Color);
			}
		}

	}
}