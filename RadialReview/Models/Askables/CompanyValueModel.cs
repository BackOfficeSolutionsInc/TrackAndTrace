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
				Map(x => x.CompanyValue);
				Map(x => x.OrganizationId);
			}
		}
	}
}