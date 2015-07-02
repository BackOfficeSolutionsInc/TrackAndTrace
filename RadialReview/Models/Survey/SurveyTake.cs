using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Accessors;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models.Survey
{
	public class SurveyTake : ILongIdentifiable, IHistorical
	{
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual SurveyRespondentModel Respondent { get; set; }
		public virtual long RespondentId { get; set; }
		public virtual SurveyContainerModel Container { get; set; }
		public virtual long ContainerId { get; set; }
		public virtual List<SurveyTakeAnswer> _Answers { get; set; }

		public virtual String UserAgent { get; set; }
		public virtual String IPAddress { get; set; }
		
		public SurveyTake()
		{
			CreateTime = DateTime.UtcNow;
		}

		public class MMap : ClassMap<SurveyTake>
		{
			public MMap()
			{
				Id(x => x.Id);
				Map(x => x.UserAgent);
				Map(x => x.IPAddress);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);

				Map(x => x.ContainerId).Column("ContainerId");
				References(x => x.Container).Column("ContainerId").Not.Nullable().ReadOnly();

				Map(x => x.RespondentId).Column("RespondentId");
				References(x => x.Respondent).Column("RespondentId").Not.Nullable().ReadOnly();
			}
		}
	}

	public class SurveyTakeAnswer : IStringIdentifiable, IHistorical
	{
		public virtual String Id { get; set; }
		public virtual int? Answer { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long SurveyTakeId { get; set; }
		public virtual SurveyTake SurveyTake { get; set; }
		public virtual long QuestionId { get; set; }
		public virtual SurveyQuestionModel Question { get; set; }
		public SurveyTakeAnswer()
		{
			CreateTime = DateTime.UtcNow;
		}

		public class MMap : ClassMap<SurveyTakeAnswer>
		{
			public MMap()
			{
				Id(x => x.Id).CustomType(typeof(string)).GeneratedBy.Custom(typeof(GuidStringGenerator)).Length(36);

				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.Answer);
				Map(x => x.SurveyTakeId).Column("SurveyTake_id");
				References(x => x.SurveyTake)
					.Column("SurveyTake_id")
					.Not.Nullable().ReadOnly();

				Map(x => x.QuestionId).Column("Question_id");
				References(x => x.Question)
					.Column("Question_id")
					.Not.Nullable()
					.Not.LazyLoad()
					.ReadOnly();
			}
		}
	}

	
}