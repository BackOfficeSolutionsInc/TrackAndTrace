using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;

namespace RadialReview.Models.Askables {
	public class RockModel :Askable {

		public virtual String Rock { get; set; }

		public virtual long OrganizationId { get; set; }
		public virtual long ForUserId { get; set; }

		public override QuestionType GetQuestionType()
		{
			return QuestionType.Rock;
		}

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
			}
		}
	}
}