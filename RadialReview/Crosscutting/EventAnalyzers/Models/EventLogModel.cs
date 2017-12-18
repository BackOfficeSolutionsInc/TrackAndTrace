using FluentNHibernate.Mapping;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Crosscutting.EventAnalyzers.Models {
	public class EventLogModel : ILongIdentifiable, IHistorical {
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }

		public String EventAnalyzerName { get; set; }
		public DateTime LastRun { get; set; }
		public EventFrequency Frequency { get; set; }

		public EventLogModel() {
			CreateTime = DateTime.UtcNow;
		}

		public class Map : ClassMap<EventLogModel> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);

				Map(x => x.EventAnalyzerName);
				Map(x => x.LastRun);
				Map(x => x.Frequency);
			}

		}
	}
}