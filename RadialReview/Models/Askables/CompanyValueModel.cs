using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;

namespace RadialReview.Models.Askables
{
	public class CompanyValueModel : Askable
	{

		public virtual string CompanyValueDetails { get; set; }
		public virtual string CompanyValue { get; set; }
		public virtual long OrganizationId { get; set; }

		public override Enums.QuestionType GetQuestionType()
		{
			return QuestionType.CompanyValue;
		}

		public override string GetQuestion()
		{
			return CompanyValue;
		}
		public class CompanyValueModelMap : SubclassMap<CompanyValueModel>
		{
			public CompanyValueModelMap()
			{
				Map(x => x.CompanyValue).Length(512);
				Map(x => x.CompanyValueDetails).Length(14000);
				Map(x => x.OrganizationId);
			}
		}
	}
}