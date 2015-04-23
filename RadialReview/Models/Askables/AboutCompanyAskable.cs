using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;

namespace RadialReview.Models.Askables
{
	public class AboutCompanyAskable : Askable
	{
		public virtual String Question { get; set; }
		public virtual QuestionType QuestionType { get; set; }
		public virtual long OrganizationId { get; set; }
		public virtual OrganizationModel Organization { get; set; }
		public override QuestionType GetQuestionType(){
			return QuestionType;
		}

		public override string GetQuestion(){
			return Question;
		}

		public class ACAMap : SubclassMap<AboutCompanyAskable>
		{
			public ACAMap()
			{
				Map(x => x.Question);
				Map(x => x.QuestionType);
				Map(x => x.OrganizationId).Column("OrganizationId");
				References(x => x.Organization).Column("OrganizationId").Not.LazyLoad().ReadOnly();
			}
		}
	}
}