using System.ComponentModel.DataAnnotations;
using Amazon.Redshift.Model;
using FluentNHibernate.Mapping;
using NHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Survey
{
	public enum SurveyQuestionType
	{
		Invalid=0,
		Scale	
	}

	public class SurveyContainerModel : ILongIdentifiable, IHistorical
	{
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime{ get; set; }
		public virtual UserOrganizationModel Creator { get; set; }
		public virtual OrganizationModel Organization { get; set; }
		public virtual SurveyQuestionGroupModel QuestionGroup { get; set; }
		public virtual SurveyRespondentGroupModel RespondentGroup { get; set; }
		[Required]
		public virtual string Name { get; set; }

		public SurveyContainerModel(){
			CreateTime = DateTime.UtcNow;
			QuestionGroup = new SurveyQuestionGroupModel();
			RespondentGroup = new SurveyRespondentGroupModel();
		}

		public class MMap : ClassMap<SurveyContainerModel> 
		{
			public MMap()
			{
				Id(x => x.Id);
				Map(x => x.Name);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				References(x => x.Creator).Not.Nullable().ReadOnly();
				References(x => x.Organization).Not.Nullable().ReadOnly();
				References(x => x.QuestionGroup).Not.Nullable().Not.LazyLoad();
				References(x => x.RespondentGroup).Not.Nullable().Not.LazyLoad();
			}
		} 
	}

	public class SurveyQuestionGroupModel : ITemporal
	{
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual bool Locked { get; set; }
		public virtual long? CopiedFrom { get; set; }

		public virtual OrganizationModel Organization { get; set; }
		public virtual string Name { get; set; }

		public virtual List<SurveyQuestionModel> _Questions { get; set; }

		public SurveyQuestionGroupModel()
		{
			CreateTime = DateTime.UtcNow;
			_Questions=new List<SurveyQuestionModel>();
		}

		public class MMap : ClassMap<SurveyQuestionGroupModel>
		{
			public MMap()
			{
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.Name);
				Map(x => x.Locked);
				Map(x => x.CopiedFrom);
				References(x => x.Organization).Not.Nullable().ReadOnly();
			}
		}
	}
	public class SurveyRespondentGroupModel : ITemporal
	{
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual DateTime? SurveyStart { get; set; }
		public virtual bool Locked { get; set; }
		public virtual long? CopiedFrom { get; set; }
		public virtual string Name { get; set; }
		public virtual OrganizationModel Organization { get; set; }

		public virtual List<SurveyRespondentModel> _Respondents { get; set; }

		public SurveyRespondentGroupModel()
		{
			CreateTime = DateTime.UtcNow;
			_Respondents = new List<SurveyRespondentModel>();
		}
		public class MMap : ClassMap<SurveyRespondentGroupModel>
		{
			public MMap()
			{
				Id(x => x.Id);
				Map(x => x.Name);
				Map(x => x.CreateTime);
				Map(x => x.SurveyStart);
				Map(x => x.DeleteTime);
				Map(x => x.Locked);
				Map(x => x.CopiedFrom);
				References(x => x.Organization).Not.Nullable().ReadOnly();
			}
		}
	}

	public class SurveyRespondentModel : ITemporal
	{
		public virtual long Id { get; set; }
		public virtual bool Locked { get; set; }
		public virtual string Email { get; set; }
		public virtual long? CopiedFrom { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual SurveyRespondentGroupModel ForRespondentGroup { get; set; }
		public virtual long ForRespondentGroupId { get; set; }

		public virtual string LookupGuid { get; set; }

		public SurveyRespondentModel()
		{
			CreateTime = DateTime.UtcNow;
		}
		public class MMap : ClassMap<SurveyRespondentModel>
		{
			public MMap()
			{
				Id(x => x.Id);
				Map(x => x.LookupGuid);
				Map(x => x.Email);
				Map(x => x.Locked);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.CopiedFrom);
				Map(x => x.ForRespondentGroupId).Column("ForRespondentGroupId");
				References(x => x.ForRespondentGroup)
					.Column("ForRespondentGroupId")
					.Not.Nullable().ReadOnly();
			}
		}
	}

	public class SurveyQuestionModel : ITemporal
	{
		public virtual long Id { get; set; }
		public virtual bool Locked { get; set; }
		public virtual string Question { get; set; }
		public virtual long? CopiedFrom { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual SurveyQuestionType QuestionType { get; set; }
		public virtual SurveyQuestionGroupModel ForQuestionGroup { get; set; }
		public virtual long ForQuestionGroupId { get; set; }

		public SurveyQuestionModel()
		{
			CreateTime = DateTime.UtcNow;
			QuestionType = SurveyQuestionType.Scale;
		}
		public class MMap : ClassMap<SurveyQuestionModel>
		{
			public MMap()
			{
				Id(x => x.Id);
				Map(x => x.Locked);
				Map(x => x.Question);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.CopiedFrom);
				Map(x => x.ForQuestionGroupId).Column("ForQuestionGroupId");
				Map(x => x.QuestionType).CustomType<SurveyQuestionType>();
				References(x => x.ForQuestionGroup)
					.Column("ForQuestionGroupId")
					.Not.Nullable().ReadOnly();
			}
		}
	}
}