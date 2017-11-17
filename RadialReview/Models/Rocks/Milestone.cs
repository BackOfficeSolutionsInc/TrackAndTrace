using FluentNHibernate.Mapping;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RadialReview.Models.Askables;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;

namespace RadialReview.Models.Rocks {


	[JsonConverter(typeof(StringEnumConverter))]
	public enum MilestoneStatus {
		NotDone = 0,
		Done = 8,
	}

	public class Milestone : ILongIdentifiable, IHistorical {
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }

		public virtual DateTime DueDate { get; set; }
		public virtual String Name { get; set; }

		public virtual MilestoneStatus Status { get; set; }
		public virtual DateTime? CompleteTime { get; set; }

		[ScriptIgnore]
		public virtual string PadId { get; set; }
		public virtual long RockId { get; set; }
		public virtual bool Required { get; set; }

		public virtual RockModel _Rock { get; set; }

		public Milestone() {
			PadId = Guid.NewGuid().ToString();
			CreateTime = DateTime.UtcNow;
			Required = true;
		}

		public class Map : ClassMap<Milestone> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.CompleteTime);
				Map(x => x.DueDate);
				Map(x => x.Name);
				Map(x => x.Status);
				Map(x => x.PadId);
				Map(x => x.RockId);
			}
		}
	}
}