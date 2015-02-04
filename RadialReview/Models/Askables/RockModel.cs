using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Periods;

namespace RadialReview.Models.Askables {
	public class RockModel :Askable {

		public virtual String Rock { get; set; }

		public virtual long OrganizationId { get; set; }
		public virtual long ForUserId { get; set; }

		public override QuestionType GetQuestionType(){
			return QuestionType.Rock;
		}

		public virtual long? PeriodId { get; set; }
		public virtual PeriodModel Period { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? CompleteTime { get; set; }

		public override string GetQuestion()
		{
			return Rock;
		}

		public class RockModelMap : SubclassMap<RockModel>
		{
			public RockModelMap()
			{
				Map(x => x.Rock);
				Map(x => x.OrganizationId);
				Map(x => x.ForUserId);
				Map(x => x.CreateTime);
				Map(x => x.CompleteTime);
				Map(x => x.PeriodId).Column("PeriodId");
				References(x => x.Period).Column("PeriodId").Not.LazyLoad().ReadOnly();
			}
		}
	}
}